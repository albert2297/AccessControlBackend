using Microsoft.AspNetCore.Identity;
using AccessControl.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace AccessControl.Tests.UnitTests.Mocks
{
    internal class MockUserStore(IEnumerable<UserEntity> users) : IUserStore<UserEntity>, IQueryableUserStore<UserEntity>, IUserPasswordStore<UserEntity>, IUserEmailStore<UserEntity>
    {
        private readonly List<UserEntity> _users = users.ToList();

        public IQueryable<UserEntity> Users => _users.AsQueryable().AsAsyncQueryable();

        public Task<IdentityResult> CreateAsync(UserEntity user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(UserEntity user, CancellationToken cancellationToken)
        {
            _users.Remove(user);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<UserEntity?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id.ToString().Equals(userId, StringComparison.OrdinalIgnoreCase)) ?? new UserEntity())!;
        }

        public Task<UserEntity?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(u => string.Equals(u.Email, normalizedUserName, StringComparison.OrdinalIgnoreCase)) ?? new UserEntity())!;
        }

        public Task<IdentityResult> UpdateAsync(UserEntity user, CancellationToken cancellationToken)
        {
            var index = _users.FindIndex(u => u.Id == user.Id);
            if (index >= 0) _users[index] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public void Dispose() { }

        public Task<string?> GetNormalizedUserNameAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email?.ToUpper() ?? string.Empty)!;
        }

        public Task<string> GetUserIdAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string?> GetUserNameAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email)!;
        }

        public Task SetNormalizedUserNameAsync(UserEntity user, string? normalizedName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(UserEntity user, string? userName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(UserEntity user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public Task SetEmailAsync(UserEntity user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task SetEmailConfirmedAsync(UserEntity user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task<UserEntity?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Email?.ToUpper() == normalizedEmail))!;
        }

        public Task<string?> GetNormalizedEmailAsync(UserEntity user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email?.ToUpper() ?? string.Empty)!;
        }

        public Task SetNormalizedEmailAsync(UserEntity user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    internal static class AsyncQueryExtensions
    {
        public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            return source.AsAsyncEnumerable();
        }

        public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
        {
            return new TestAsyncEnumerable<T>(source);
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(inner.MoveNext());
        }

        public T Current => inner.Current;
    }

    internal class TestAsyncQueryProvider<T>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<T>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return inner.Execute(expression)!;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var resultType = typeof(TResult).GetGenericArguments().First();
            var executeMethod = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: [typeof(Expression)]);

            _ = executeMethod ?? throw new InvalidOperationException("The Execute method could not be found.");

            var executionResult = executeMethod
                .MakeGenericMethod(resultType)
                .Invoke(this, [expression]);

            _ = executionResult ?? throw new InvalidOperationException("Execution result is null.");

            var fromResultMethod = typeof(Task)
                .GetMethod(nameof(Task.FromResult))?
                .MakeGenericMethod(resultType);

            _ = fromResultMethod ?? throw new InvalidOperationException("The FromResult method could not be found.");

            var taskResult = fromResultMethod.Invoke(null, [executionResult]);

            _ = taskResult ?? throw new InvalidOperationException("Task result is null.");

            return (TResult)taskResult;
        }
    }
}
