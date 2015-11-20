using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data;

namespace DBCViewer
{
    class FilterExpressions
    {
        public static bool Compare(Type type, ComparisonType comp, DataRow data)
        {
            //ParameterExpression pe = Expression.Parameter(type, 
            return false;
        }

        delegate bool Test(object a, object b);

        private static bool Equal(object a, object b)
        {
            Expression left = Expression.Constant(a);
            Expression right = Expression.Constant(b);
            //Expression equal = Expression.Equal(left, right);
            return Expression.Lambda<Test>(Expression.MakeBinary(ExpressionType.Equal, left, right)).Compile()(a, b);
        }
    }
}
