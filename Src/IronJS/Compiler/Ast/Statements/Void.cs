﻿using System;
using System.Text;
using Antlr.Runtime.Tree;
using IronJS.Runtime2.Js;
using System.Collections.Generic;

#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif

namespace IronJS.Compiler.Ast
{
    public class Void : Node
    {
        public INode Target { get; protected set; }

        public Void(INode target, ITree node)
            : base(NodeType.Void, node)
        {
            Target = target;
        }

        public override Type Type
        {
            get
            {
                return IjsTypes.Undefined;
            }
        }

        public override INode Analyze(Stack<Function> astopt)
        {
            Target = Target.Analyze(astopt);
            return this;
        }

        public override void Write(StringBuilder writer, int indent)
        {
            string indentStr = new String(' ', indent * 2);

            writer.AppendLine(indentStr + "(" + NodeType);

            if (Target != null)
                Target.Write(writer, indent + 1);

            writer.AppendLine(indentStr + ")");
        }
    }
}