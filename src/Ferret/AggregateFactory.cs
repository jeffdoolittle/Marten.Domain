using System;
using System.Collections.Generic;
using System.Linq;

namespace Ferret
{
    public class AggregateFactory
    {
        private static object _lock = new object();
        private static IList<string> ScannedAssemblies = new List<string>();
        private static readonly Dictionary<Type, Type> StateToAggregateTypeMap = new Dictionary<Type, Type>();

        public IAggregate Create<TState>(TState state)
            where TState : class, IState
        {
            var assemblyName = state.GetType().Assembly.FullName;
            if (!ScannedAssemblies.Contains(assemblyName))
            {
                Scan(typeof(TState));
                ScannedAssemblies.Add(assemblyName);
            }

            var aggregateType = StateToAggregateTypeMap[typeof(TState)];
            return (IAggregate)Activator.CreateInstance(aggregateType, new object[] { state });
        }

        private void Scan(Type type)
        {
            lock(_lock)
            {
                if (StateToAggregateTypeMap.ContainsKey(type))
                {
                    return;
                }

                var aggregateTypes = type.Assembly
                    .GetExportedTypes()
                    .Where(x => !x.IsInterface && !x.IsAbstract && x.GetInterfaces()
                        .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IAggregate<>)));

                foreach (var aggregateType in aggregateTypes)
                {
                    var i = aggregateType.GetInterfaces().Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAggregate<>));
                    var modelType = i.GetGenericArguments()[0];
                    StateToAggregateTypeMap.Add(modelType, aggregateType);
                }
            }
        }
    }
}
