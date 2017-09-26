﻿using PT.PM.Common;
using PT.PM.Common.Nodes;
using PT.PM.Common.Nodes.Expressions;

namespace PT.PM.Matching.Patterns
{
    public class PatternAssignmentExpression : PatternBase
    {
        public PatternBase Left { get; set; }

        public PatternBase Right { get; set; }

        public PatternAssignmentExpression()
        {
        }

        public PatternAssignmentExpression(PatternBase left, PatternBase right,
            TextSpan textSpan = default(TextSpan))
            : base(textSpan)
        {
            Left = left;
            Right = right;
        }

        public override Ust[] GetChildren() => new Ust[] { Left, Right };

        public override string ToString()
        {
            return Right == null ? Left.ToString() : $"{Left} = {Right}";
        }

        public override MatchingContext Match(Ust ust, MatchingContext context)
        {
            MatchingContext match;

            if (ust is AssignmentExpression assign)
            {
                match = Left.Match(assign.Left, context);
                if (match.Success)
                {
                    match = Right.Match(assign.Right, match);
                }
            }
            else
            {
                match = context.Fail();
            }

            return match.AddUstIfSuccess(ust);
        }
    }
}
