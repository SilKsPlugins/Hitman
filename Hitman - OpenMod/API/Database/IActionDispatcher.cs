using OpenMod.API.Ioc;
using System;
using System.Threading.Tasks;

namespace Hitman.API.Database
{
    [Service]
    public interface IActionDispatcher
    {
        public Task Enqueue(Action action, Action<Exception>? exceptionHandler = null);
        public Task Enqueue(Func<Task> task, Action<Exception>? exceptionHandler = null);

        public Task<T> Enqueue<T>(Func<T> action, Action<Exception>? exceptionHandler = null);
        public Task<T> Enqueue<T>(Func<Task<T>> task, Action<Exception>? exceptionHandler = null);
    }
}
