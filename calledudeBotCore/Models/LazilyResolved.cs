using Microsoft.Extensions.DependencyInjection;
using System;

namespace calledudeBot.Models;

public class LazilyResolved<T> : Lazy<T> where T : notnull
{
    public LazilyResolved(IServiceProvider serviceProvider)
        : base(serviceProvider.GetRequiredService<T>)
    {
    }
}
