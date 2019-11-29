using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Ddd.Infrastructure
{
	/// <summary>
	/// Базовый класс для всех Value типов.
	/// </summary>
	public class ValueType<T> where T : class
	{
        private readonly Type currentType;
        private readonly PropertyInfo[] properties;
        public ValueType()
        {
            currentType = typeof(T);
            properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        public override bool Equals(object obj) => Equals(obj as T);
      
        public bool Equals(T obj)
        {
            if (obj == null) return false;
            return properties.All((p) =>
            {
                var leftValue = p.GetValue(this);
                var rigthValue = p.GetValue(obj);
                return leftValue == null && rigthValue == null ||
                       leftValue != null && leftValue.Equals(rigthValue);
            });    
        }

        public override int GetHashCode()
        {
            int hashCode = 354564456;
            unchecked
            {
                foreach (var propety in properties)
                {
                    hashCode ^= ((hashCode << 4) +
                                propety.GetValue(this).GetHashCode() +
                                (hashCode >> 7));
                }
                hashCode = -1521134295 * (-1521134295 * hashCode);
            }
            return hashCode;
        }

        public override string ToString()
        {
            var lexicograficallyProperties = properties.OrderBy(p => p.Name);
            var builder = new StringBuilder();
            builder.Append($"{currentType.Name}(");
            foreach(var property in lexicograficallyProperties)
            {
                builder.Append($"{property.Name}: {property.GetValue(this)}; ");
            }
            builder.Remove(builder.Length - 2, 2);
            builder.Append(")");
            return builder.ToString();
        }
    }
}