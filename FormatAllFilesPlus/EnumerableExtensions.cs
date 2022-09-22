using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FormatAllFilesPlus
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            ArgumentNotNull(source, "source");
            ArgumentNotNull(action, "action");

            foreach (T each in source)
            {
                action(each);
            }
        }

        public static IEnumerable<T> Recursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> getChildren)
        {
            ArgumentNotNull(source, "source");
            ArgumentNotNull(getChildren, "getChildren");

            foreach (var item in source)
            {
                yield return item;
            }
            foreach (var item in source)
            {
                var results = SearchBreadthFirst(item, getChildren);
                foreach (var result in results)
                {
                    yield return result;
                }
            }

            yield break;
        }

        private static void ArgumentNotNull<T>(T argumentValue, string argumentName) where T : class
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        private static IEnumerable<T> SearchBreadthFirst<T>(T source, Func<T, IEnumerable<T>> getChildren)
        {
            if (source == null)
            {
                yield break;
            }

            var queue = new Queue<T>();
            Action<T> addChild = item =>
            {
                var children = getChildren(item);
                if (children != null)
                {
                    children.ForEach(queue.Enqueue);
                }
            };

            addChild(source);

            while (queue.Any())
            {
                var current = queue.Dequeue();
                T target = current;
                if (target != null)
                {
                    yield return target;
                }

                addChild(current);
            }
        }
    }
}
