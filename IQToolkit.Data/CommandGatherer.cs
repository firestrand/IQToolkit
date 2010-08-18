using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data
{
    using Common;
    using Mapping;
    sealed class CommandGatherer : DbExpressionVisitor
    {
        readonly List<QueryCommand> _commands = new List<QueryCommand>();

        public static IEnumerable<QueryCommand> Gather(Expression expression)
        {
            var gatherer = new CommandGatherer();
            gatherer.Visit(expression);
            return gatherer._commands.AsReadOnly();
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            QueryCommand qc = c.Value as QueryCommand;
            if (qc != null)
            {
                this._commands.Add(qc);
            }
            return c;
        }
    }
}
