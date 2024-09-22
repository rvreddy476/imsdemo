using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models
{
   public class ReflectionClass
    {
        public static class SimpleComparer
        {



            // Item1: property name, Item2 current, Item3 original
            public static List<Tuple<string, object, object>> Differences<T>(T current, T original)
            {
                var diffs = new List<Tuple<string, object, object>>();



                MethodInfo areEqualMethod = typeof(SimpleComparer).GetMethod("AreEqual", BindingFlags.Static | BindingFlags.NonPublic);

                foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    object x = prop.GetValue(current);
                    object y = prop.GetValue(original);
                    bool areEqual = (bool)areEqualMethod.MakeGenericMethod(prop.PropertyType).Invoke(null, new object[] { x, y });

                    if (!areEqual)
                    {
                        diffs.Add(Tuple.Create(prop.Name, x, y));
                    }
                }

                return diffs;
            }

            private static bool AreEqual<T>(T x, T y)
            {
                return EqualityComparer<T>.Default.Equals(x, y);
            }


        }
    }
}
