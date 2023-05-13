using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Svelto.Common;

namespace AkroGame.ECS.Websocket
{
    public static class ReflectionUtil
    {
        private static readonly MethodInfo copyMethod = typeof(Unsafe)
            .GetMethods()
            .Single(
                x => x.Name == "Copy" && x.GetParameters().First().ParameterType == typeof(void*)
            );

        public static T GetPrivateField<S, T>(this S thisObj, string name) where T : class
        {
            var field = typeof(S).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException(
                    $"{name} is not a valid private field of {typeof(T).Name}"
                );
            var instanceField = field.GetValue(thisObj);
            if (!(instanceField is T tmp))
                throw new ArgumentException(
                    $"{name} is not of type {typeof(T)}, it's {field.GetType()}"
                );
            return tmp;
        }

        public static void WriteToUnsafeMemory(IntPtr array, Type t, object component, uint index)
        {
            MethodInfo? sizeOfMethod = typeof(MemoryUtilities).GetMethod(
                "SizeOf",
                Array.Empty<Type>()
            )?.MakeGenericMethod(new Type[] { t });

            var sizeB = sizeOfMethod?.Invoke(null, null);
            if (sizeB == null)
                return;
            var offset = (int)((int)sizeB * index);

            copyMethod.MakeGenericMethod(new Type[] { t })?.Invoke(
                null,
                new object[] { IntPtr.Add(array, offset), component }
            );
        }
    }
}
