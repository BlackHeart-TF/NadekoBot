﻿using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace NadekoBot.Common.Yml
{
    public class CommentsObjectGraphVisitor : ChainedObjectGraphVisitor
    {
        public CommentsObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
            : base(nextVisitor)
        {
        }

        public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context,ObjectSerializer serializer)
        {
            var commentsDescriptor = value as CommentsObjectDescriptor;
            if (commentsDescriptor != null && !string.IsNullOrWhiteSpace(commentsDescriptor.Comment))
            {
                context.Emit(new Comment(commentsDescriptor.Comment.Replace("\n", "\n# "), false));
            }

            return base.EnterMapping(key, value, context, serializer);
        }
    }
}
