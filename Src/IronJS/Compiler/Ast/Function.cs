/* ****************************************************************************
 *
 * Copyright (c) Fredrik Holmström
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using Antlr.Runtime.Tree;
using IronJS.Runtime2.Js;
using IronJS.Tools;
using Microsoft.Scripting.Utils;

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace IronJS.Compiler.Ast {

    #region Aliases
    using Et = Expression;
    #endregion

    public class Function : Node {
        public INode Name { get; private set; }
        public INode Body { get; private set; }
        public Dictionary<string, Variable> Variables { get; private set; }
        public string[] ParameterNames { get; private set; }
        public bool IsLambda { get { return Name == null; } }
        public Type ReturnType { get { return IjsTypes.Dynamic; } }
        public override Type Type { get { return IjsTypes.Object; } }

        /*
         * Compilation properties
         **/
        public Et Globals {
            get {
                return Et.Field(this["//closure"].Expr, "Globals");
            }
        }

        public Et Context {
            get {
                return Et.Field(this["//closures"].Expr, "Context");
            }
        }

        public LabelTarget ReturnLabel {
            get;
            set;
        }

        public Function(INode name, List<string> parameters, INode body, ITree node)
            : base(NodeType.Func, node) {
            Body = body;
            Name = name;
            Variables = new Dictionary<string, Variable>();
            ParameterNames = ArrayUtils.Insert("//closures", ArrayUtils.MakeArray(parameters));

            this["//closure"] = new Parameter("//closures");
            this["//closure"].ForceType(typeof(IjsClosure));

            if (parameters != null) {
                foreach (var param in parameters) {
                    this[param] = new Parameter(param);
                }
            }
        } 

        public override INode Analyze(Stack<Function> stack) {
            if (!IsLambda) {
                Name = Name.Analyze(stack);
            }

            stack.Push(this);
            Body = Body.Analyze(stack);
            stack.Pop();

            return this;
        }

        public override Expression Compile(Function func) {
            return AstTools.New(
                typeof(IjsFunc),
                AstTools.Constant(this),
                AstTools.New(
                    typeof(IjsClosure),
                    func.Context,
                    func.Globals
                )
            );
        }

        public Variable this[string name] {
            get {
                return Variables[name];
            }
            set {
                Variables[name] = value;
            }
        }

        public Variable this[int argn] {
            get {
                return Variables[ParameterNames[argn]];
            }
            set {
                Variables[ParameterNames[argn]] = value;
            }
        }

        public override void Write(StringBuilder writer, int depth) {
            string indent = StringTools.Indent(depth * 2);
            string indent1 = StringTools.Indent((depth + 1) * 2);

            writer.AppendLine(indent + "(Function");

            if (!IsLambda) {
                Name.Write(writer, depth + 1);
            }

            writer.AppendLine(indent1 + "(Variables");
            foreach (Variable variable in Variables.Values) {
                variable.Write(writer, depth + 2);
            }
            writer.AppendLine(indent1 + ")");

            Body.Write(writer, depth + 1);

            writer.AppendLine(indent + ")");
        }
    }
}