using System;
using System.Collections.Generic;
using System.Linq;
using Category.Theory.Linq;

namespace Category.Theory.Monads
{
    public static class MaybeExtensions
    {
        /// <summary>
        /// Get the value from the given maybe of throw the given exception
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static T GetValueOrThrow<T>(this Maybe<T> maybe, Exception ex)
        {
            if (maybe.TryGetValue(out T value))
            {
                return value;
            }

            throw ex;
        }

        /// <summary>
        /// Get the value from the given maybe or a generic exception is thrown
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static T GetValueOrThrow<T>(this Maybe<T> maybe, string errorMessage = null)
        {
            if (maybe.TryGetValue(out T value))
            {
                return value;
            }

            throw new InvalidOperationException(errorMessage ?? $"No value set on maybe");
        }

        /// <summary>
        /// Get the value from the given maybe or the provided fallback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="fallbackValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static T GetValueOrFallback<T>(this Maybe<T> maybe, T fallbackValue)
        {
            if (fallbackValue == null)
            {
                throw new ArgumentNullException(nameof(fallbackValue));
            }

            if (maybe.TryGetValue(out T value))
            {
                return value;
            }

            return fallbackValue;
        }

        /// <summary>
        /// Monadic join (where inner monad is Nullable)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Maybe<TResult> SelectMany<T, TResult>(
            this Maybe<T> maybe,
            Func<T, TResult?> selector) where TResult : struct
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return maybe.SelectMany(e => NullableToMaybe(selector(e)));
        }

        /// <summary>
        /// Enables query syntaxt for maybe
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="tResult"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Maybe<TResult> SelectMany<T1, T2, TResult>(
            this Maybe<T1> t1,
            Func<T1, Maybe<T2>> t2,
            Func<T1, T2, TResult> tResult)
        {
            if (t1 == null)
            {
                throw new ArgumentNullException(nameof(t1));
            }

            if (t2 == null)
            {
                throw new ArgumentNullException(nameof(t2));
            }

            if (tResult == null)
            {
                throw new ArgumentNullException(nameof(tResult));
            }

            return t1.SelectMany(x => t2(x).Select(y => tResult(x, y)));
        }

        /// <summary>
        /// Monadic flatmap
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <returns></returns>
        public static Maybe<T> FlatMap<T>(this Maybe<Maybe<T>> maybe)
        {
            if (maybe.TryGetValue(out Maybe<T> inner))
            {
                return inner;
            }

            return None<T>.Instance;
        }

        /// <summary>
        /// Execute the appropriate action based on the maybe content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="someAction"></param>
        /// <param name="noneAction"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Iter<T>(
            this Maybe<T> maybe,
            Action<T> someAction,
            Action noneAction)
        {
            if (someAction == null)
            {
                throw new ArgumentNullException(nameof(someAction));
            }

            if (maybe.TryGetValue(out T value))
            {
                someAction(value);
            }
            else
            {
                noneAction();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Maybe<T> Where<T>(this Maybe<T> maybe, Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (maybe.TryGetValue(out T value) && predicate(value))
            {
                return value;
            }

            return None<T>.Instance;
        }

        public static Maybe<T> IfSome<T>(this Maybe<T> maybe, Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (maybe.TryGetValue(out T value))
            {
                action.Invoke(value);
            }

            return maybe;
        }

        /// <summary>
        /// Convert the given maybe to nullable (for value types)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <returns></returns>
        public static T? ToNullable<T>(this Maybe<T> maybe) where T : struct
        {
            if (maybe.HasValue())
            {
                return maybe.GetValueOrThrow();
            }

            return null;
        }

        /// <summary>
        /// Select a string from the given maybe.
        /// Assure the string is not null or empty spaces.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static Maybe<string> SelectString<T>(this Maybe<T> maybe, Func<T, string> selector)
        {
            return maybe.SelectMany(e => TrySelectString(e, selector));
        }

        /// <summary>
        /// Try select a string from the given object.
        /// Assure the string is not null or empty spaces.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="candidate"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static Maybe<string> TrySelectString<T>(this T candidate, Func<T, string> selector)
        {
            if (candidate == null)
            {
                return Maybe.None<string>();
            }

            string stringValue = selector(candidate);
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return Maybe.Some(stringValue);
            }

            return Maybe.None<string>();
        }

        /// <summary>
        /// Select only items in the list of maybe
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<T> SelectItems<T>(this IEnumerable<Maybe<T>> items)
        {
            if (items.NullOrEmpty())
            {
                return Enumerable.Empty<T>();
            }

            return items
                .Where(i => i.HasValue())
                .Select(i => i.GetValueOrThrow());
        }

        /// <summary>
        /// Select the element given the key in the dictionary
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Maybe<T> Select<K, T>(this IDictionary<K, T> dictionary, K key)
        {
            if (dictionary.NullOrEmpty())
            {
                return Maybe.None<T>();
            }

            T value;
            if (dictionary.TryGetValue(key, out value))
            {
                return Maybe.Some(value);
            }

            return Maybe.None<T>();
        }

        /// <summary>
        /// Select the maybe element given the key in the dictionary
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Maybe<T> Select<K, T>(this IDictionary<K, Maybe<T>> dictionary, K key)
        {
            if (dictionary.NullOrEmpty())
            {
                return Maybe.None<T>();
            }

            Maybe<T> value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return Maybe.None<T>();
        }

        /// <summary>
        /// Select the item at the given index in case the index is defined.
        /// Otherwise return an empty object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public static Maybe<T> Select<T>(this IList<T> source, int idx)
        {
            if (source.NullOrEmpty())
            {
                // Empty collection
                return Maybe.None<T>();
            }

            if (idx < 0 || idx >= source.Count)
            {
                // Index out of bound
                return Maybe.None<T>();
            }

            return Maybe.Some(source[idx]);
        }

        /// <summary>
        /// Check whether the given source collection contains one and only one element.
        /// In that case return the element, otherwise returns an empty object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Maybe<T> TrySingle<T>(this IEnumerable<T> source)
        {
            T element;
            if (source.IsSingle(out element))
            {
                return Maybe.Some(element);
            }

            return Maybe.None<T>();
        }

        /// <summary>
        /// Return the first occurrence of the element in the source
        /// or an empty object in case the list is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Maybe<T> TryFirst<T>(this IEnumerable<T> source)
        {
            if (source.NullOrEmpty())
            {
                return Maybe.None<T>();
            }

            return Maybe.Some(source.First());
        }

        private static Maybe<T> NullableToMaybe<T>(T? nullableValue) where T : struct
        {
            if (nullableValue.HasValue)
            {
                return new Some<T>(nullableValue.Value);
            }
            return None<T>.Instance;
        }

        /// <summary>
        /// Natural transformation from <see cref="Maybe"/> into <see cref="Either"/>
        /// </summary>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="maybe"></param>
        /// <param name="left"></param>
        /// <returns></returns>
        public static Either<TLeft, T> ToEither<TLeft, T>(this Maybe<T> maybe, TLeft left)
        {
            return maybe.Match(
                someFunc: e => Either.Right<TLeft, T>(e),
                noneFunc: () => Either.Left<TLeft, T>(left));
        }

        public static Either<TLeft, Maybe<T>> ToOptionalEither<TLeft, T>(this Maybe<T> maybe, TLeft left)
        {
            return maybe.Match(
                someFunc: e => Either.Right<TLeft, Maybe<T>>(Maybe.Some(e)),
                noneFunc: () => Either.Left<TLeft, Maybe<T>>(left));
        }
    }
}