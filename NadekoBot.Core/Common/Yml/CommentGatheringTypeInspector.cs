using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace NadekoBot.Common.Yml
{
    public class CommentGatheringTypeInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector innerTypeDescriptor;

        public CommentGatheringTypeInspector(ITypeInspector innerTypeDescriptor)
        {
            this.innerTypeDescriptor = innerTypeDescriptor ?? throw new ArgumentNullException("innerTypeDescriptor");
        }

        

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            return innerTypeDescriptor
                .GetProperties(type, container)
                .Select(d => new CommentsPropertyDescriptor(d));
        }

        public override string GetEnumName(Type enumType, string name)
        {
            throw new NotImplementedException();
        }

        public override string GetEnumValue(object enumValue)
        {
            throw new NotImplementedException();
        }

        private sealed class CommentsPropertyDescriptor : IPropertyDescriptor
        {
            private readonly IPropertyDescriptor baseDescriptor;

            public CommentsPropertyDescriptor(IPropertyDescriptor baseDescriptor)
            {
                this.baseDescriptor = baseDescriptor;
                Name = baseDescriptor.Name;
            }

            public string Name { get; set; }

            public Type Type { get { return baseDescriptor.Type; } }

            public Type TypeOverride {
                get { return baseDescriptor.TypeOverride; }
                set { baseDescriptor.TypeOverride = value; }
            }

            public int Order { get; set; }

            public ScalarStyle ScalarStyle {
                get { return baseDescriptor.ScalarStyle; }
                set { baseDescriptor.ScalarStyle = value; }
            }

            public bool CanWrite { get { return baseDescriptor.CanWrite; } }

            public bool AllowNulls { get; set; }

            public bool Required { get; set; }

            public Type ConverterType { get; set; }

            public void Write(object target, object value)
            {
                baseDescriptor.Write(target, value);
            }

            public T GetCustomAttribute<T>() where T : Attribute
            {
                return baseDescriptor.GetCustomAttribute<T>();
            }

            public IObjectDescriptor Read(object target)
            {
                var comment = baseDescriptor.GetCustomAttribute<CommentAttribute>();
                return comment != null
                    ? new CommentsObjectDescriptor(baseDescriptor.Read(target), comment.Comment)
                    : baseDescriptor.Read(target);
            }
        }
    }
}
