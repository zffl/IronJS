﻿module IronJS.Compiler.Core

open IronJS
open IronJS.Aliases
open IronJS.Tools
open IronJS.Tools.Dlr
open IronJS.Compiler

(*Adds initilization expressions for variables that should be Undefined*)
let private addUndefinedInitExprs (variables:Ast.LocalMap) (body:Et list) =
  variables |> Map.toSeq
            |> Seq.filter (fun pair -> (snd pair).InitUndefined)
            |> Seq.fold (fun state pair -> (Js.assign (snd pair).Expr Runtime.Undefined.InstanceExpr) :: state) body

(*Adds initilization expression for variables that are closed over, creating their strongbox instance*)
let private addStrongBoxInitExprs (variables:Ast.LocalMap) (body:Et list) =
  variables |> Map.toSeq
            |> Seq.filter (fun pair -> (snd pair).ClosedOver)
            |> Seq.fold (fun state pair -> (Dlr.Expr.assign (snd pair).Expr (Dlr.Expr.newInstance (snd pair).Expr.Type)) :: state) body

(*Adds initilization expressions for closed over parameters, fetching their proxy parameters value*)
let private addProxyParamInitExprs (parms:Ast.LocalMap) (proxies:Map<string, EtParam>) body =
  parms |> Map.fold (fun state name (var:Ast.Local) -> Js.assign var.Expr proxies.[name] :: state) body 

(**)
let private addDynamicScopesInitExpr (ctx:Context) (body:Et list) =
  if not ctx.Scope.HasDynamicScopes 
    then body
    else Dlr.Expr.assign ctx.LocalScopes (Dlr.Expr.newInstanceT<Runtime.Object ResizeArray>) :: body

(**)
let private addDynamicScopesLocal (ctx:Context) (vars:EtParam list) =
  if not ctx.Scope.HasDynamicScopes
    then vars
    else ctx.LocalScopes :: vars

let private addClosureInitExpr (ctx:Context) (body:Et list) =
  if ctx.ClosureAccess > 0 then
    if ctx.Closure.Type = Runtime.Closure.TypeDef then
      Expr.assign ctx.Closure (Expr.field ctx.Function "Closure") :: body
    else
      Expr.assign ctx.Closure (Expr.cast2 ctx.Closure.Type (Expr.field ctx.Function "Closure")) :: body
  else
    body

(*Gets the proper parameter list with the correct proxy replacements*)
let private getParameterListExprs (parameters:Ast.LocalMap) (proxies:Map<string, EtParam>) =
  [for kvp in parameters -> if kvp.Value.ClosedOver then proxies.[kvp.Key] else kvp.Value.Expr]
  
let private createProxyParameter name (var:Ast.Local) =
  Dlr.Expr.param ("~" + name + "_proxy") (Utils.Type.jsToClr var.UsedAs)

let private partitionParamsAndVars _ (var:Ast.Local) = 
  var.IsParameter && not var.InitUndefined

(*Compiles a Ast.Node tree into a DLR Expression-tree*)
let compileAst (env:Runtime.IEnvironment) (closureType:ClrType) (scope:Ast.Scope) (ast:Ast.Node) =

  let ctx = {
    Context.New with
      ClosureParam = Dlr.Expr.param "~closure" closureType
      Scope = scope
      Builder = Compiler.ExprGen.builder
      TemporaryTypes = new Dict<string, ClrType>()
      Env = env
  }

  let body = [
    (Compiler.ExprGen.builder ctx ast)
    (Dlr.Expr.labelExprVal ctx.Return (Expr.constant Runtime.Utils.Box.nullBox))
  ]

  let parameters, variables = ctx.Scope.Locals |> Map.partition partitionParamsAndVars
  let closedOverParameters = parameters |> Map.filter (fun _ var -> var.ClosedOver)
  let proxyParameters = closedOverParameters |> Map.map createProxyParameter
  let inputParameters = getParameterListExprs parameters proxyParameters
  let parameters = ctx.Function :: ctx.This :: inputParameters

  let localVariableExprs = 
       ctx.GlobalsParam 
    :: ctx.Closure 
    :: (closedOverParameters 
        |> Map.fold (fun state _ var -> var.Expr :: state) [for kvp in variables -> kvp.Value.Expr] 
        |> addDynamicScopesLocal ctx)

  let completeBodyExpr = 
      (if ctx.GlobalAccess > 0 then Expr.assign ctx.Globals (Expr.field ctx.Environment "Globals") else Expr.empty)
   :: (body 
      |> addUndefinedInitExprs variables
      |> addProxyParamInitExprs closedOverParameters proxyParameters
      |> addStrongBoxInitExprs ctx.Scope.Locals
      |> addDynamicScopesInitExpr ctx
      |> addClosureInitExpr ctx)

  #if INTERACTIVE
  let lmb = Dlr.Expr.lambda parameters (Dlr.Expr.blockWithLocals localVariableExprs completeBodyExpr), [for p in inputParameters -> p.Type]
  printf "%A" (Fsi.dbgViewProp.GetValue((fst lmb) :> Et, null))
  lmb
  #else
  Dlr.Expr.lambda parameters (Dlr.Expr.blockWithLocals localVariableExprs completeBodyExpr), [for p in inputParameters -> p.Type]
  #endif