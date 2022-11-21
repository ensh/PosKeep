
namespace Vtb.PosKeep.Entity
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public static class PropertyStorage<T> where T: EP
    {
        public static void Init(Type storageType, string[] storages)
        {
            if (Converters != null)
            {
                Converters.Clear();
            }
            else
            {
                Converters = new ConcurrentDictionary<Type, Func<int, object, int>>();
            }

            Storages = storages;
            StorageType = storageType;
        }

        private static Type StorageType { get; set; }
        private static string[] Storages { get; set; }
        private static ConcurrentDictionary<Type, Func<int, object, int>> Converters;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Create(int number, object position)
        {
            var convertAction = Converters.GetOrAdd(position.GetType(), t => convertActionGetter(t));
            return convertAction(number, position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Func<int, object, int> convertActionGetter(Type fromType)
        {
            var index = Expression.Parameter(typeof(int), "i");
            var entity = Expression.Parameter(typeof(object), "entity");

            var function = Expression.Lambda<Func<int, object, int>>(
                Expression.Block(assignGetter(fromType, index, entity)), index, entity);

            return function.Compile();
        }

        private static IEnumerable<Expression> assignGetter(Type fromType, ParameterExpression index, ParameterExpression entity)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var returnTarget = Expression.Label(typeof(int));

            foreach (var property in fromType.GetProperties())
            {
                var storageName = string.Concat("s_", property.Name);
                if (Storages.Contains(storageName))
                {
                    var field = StorageType.GetField(storageName, flags);
                    var storage = Expression.Field(null, field);
                    yield return Expression.Assign(
                        Expression.ArrayAccess(storage, index),
                        Expression.Convert(Expression.Property(Expression.Convert(entity, fromType), property), field.FieldType.GetElementType())
                    );
                }
            }

            yield return Expression.Return(returnTarget, index, typeof(int));
            yield return Expression.Label(returnTarget, Expression.Constant(default(int)));
        }
    }
}
