﻿using PT.PM.Common.Nodes.Expressions;
using PT.PM.Common.Nodes.Statements;

namespace PT.PM.Common.Nodes.Specific
{
    public class LockStatement : SpecificStatement
    {
        public Expression Lock { get; set; }

        public Statement Embedded { get; set; }

        public LockStatement(Expression lockExpression, Statement embedded, TextSpan textSpan)
            : base(textSpan)
        {
            Lock = lockExpression;
            Embedded = embedded;
        }

        public LockStatement()
        {
        }

        public override Ust[] GetChildren()
        {
            return new Ust[] {Lock, Embedded};
        }
    }
}
