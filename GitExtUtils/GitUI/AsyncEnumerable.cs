using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft;

namespace GitUI
{
    public static class AsyncEnumerable
    {
        public static async Task<TSource> FirstOrDefaultAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, Task<bool>> predicateAsync, bool continueOnCapturedContext = true)
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(predicateAsync, nameof(predicateAsync));

            foreach (var item in source)
            {
                if (await predicateAsync(item).ConfigureAwait(continueOnCapturedContext))
                {
                    return item;
                }
            }

            return default;
        }
    }
}
