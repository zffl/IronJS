﻿namespace IronJS.Compiler

open IronJS
open IronJS.Aliases
open IronJS.Tools
open IronJS.Compiler

open System.Dynamic

module Assign = 

    let internal build (ctx:Context) left (value:Et) =
      match left with
      | Ast.Global(name, globalScopeLevel)  -> 
        if globalScopeLevel = 0 
          then ctx.TemporaryTypes.[name] <- value.Type
               Variables.Global.assign ctx name value
          else DynamicScope.setGlobalValue ctx name value

      | Ast.Local(name, localScopeLevel)    -> 
        if localScopeLevel = 0 
          then ctx.TemporaryTypes.[name] <- value.Type
               Variables.Local.assign ctx name value
          else DynamicScope.setLocalValue ctx name value

      | Ast.Closure(name, globalScopeLevel) -> 
        if globalScopeLevel = 0 
          then ctx.TemporaryTypes.[name] <- value.Type
               Variables.Closure.assign ctx name value
          else DynamicScope.setClosureValue ctx name value

      | Ast.Property(target, name) -> CallSites.setMember (ctx.Builder ctx target) name value
      | _ -> failwith "Assignment for '%A' is not defined" left