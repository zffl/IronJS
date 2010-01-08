﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronJS.Runtime.Js
{
    using Et = System.Linq.Expressions.Expression;
    using Meta = System.Dynamic.DynamicMetaObject;
    using Restrict = System.Dynamic.BindingRestrictions;
    using AstUtils = Microsoft.Scripting.Ast.Utils;

    using System;
    using System.Dynamic;
    using System.Collections.Generic;
    using IronJS.Runtime.Utils;

    class FrameMeta<T> : DynamicMetaObject
    {
        public FrameMeta(Et parameter, object value)
            : base(parameter, Restrict.Empty, value)
        {

        }

        public override Meta BindSetMember(SetMemberBinder binder, Meta value)
        {
            var keyExpr = Et.Constant(binder.Name);
            var valueExpr = EtUtils.Box(value.Expression);

            var target = Et.Call(
                Et.Convert(this.Expression, typeof(Frame<T>)),
                typeof(Frame<T>).GetMethod("Push"),
                keyExpr,
                valueExpr
            );

            var restrictions =
                Restrict.GetTypeRestriction(
                    this.Expression,
                    this.LimitType
                );

            return new Meta(target, restrictions);
        }

        public override Meta BindGetMember(GetMemberBinder binder)
        {
            var callExpr =
                Et.Call(
                    Et.Convert(this.Expression, typeof(Frame<T>)),
                    typeof(Frame<T>).GetMethod("Pull"),
                    Et.Constant(binder.Name)
                );

            var restrictions =
                Restrict.GetTypeRestriction(
                    this.Expression,
                    this.LimitType
                );

            return new Meta(callExpr, restrictions);
        }
    }

}