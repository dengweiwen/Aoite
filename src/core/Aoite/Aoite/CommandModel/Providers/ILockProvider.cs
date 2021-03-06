﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Aoite.CommandModel
{
    /// <summary>
    /// 定义一个提供锁功能的提供程序。
    /// </summary>
    public interface ILockProvider : IContainerProvider
    {
        /// <summary>
        /// 提供锁的功能。
        /// </summary>
        /// <param name="key">锁的键名。</param>
        /// <param name="timeout">锁的超时设定。</param>
        /// <returns>返回一个锁。</returns>
        IDisposable Lock(string key, TimeSpan? timeout = null);
    }

    /// <summary>
    /// 表示一个提供锁功能的提供程序。
    /// </summary>
    [SingletonMapping]
    public class LockProvider : CommandModelContainerProviderBase, ILockProvider
    {
        /// <summary>
        /// 初始化一个 <see cref="Aoite.CommandModel.LockProvider"/> 类的新实例。
        /// </summary>
        /// <param name="container">服务容器。</param>
        public LockProvider(IIocContainer container) : base(container) { }

        /// <summary>
        /// 提供锁的功能。
        /// </summary>
        /// <param name="key">锁的键名。</param>
        /// <param name="timeout">锁的超时设定。</param>
        /// <returns>返回一个锁。</returns>
        public virtual IDisposable Lock(string key, TimeSpan? timeout = null)
        {
            if(string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var redisProvider = this.Container.GetFixedService<IRedisProvider>();
            if(redisProvider != null)
            {
                var client = this.Container.GetService<IRedisProvider>().GetRedisClient();
                return client.Lock(key, timeout);
            }

            key = "$LockProvider::" + key;
            var realTimeout = timeout ?? RedisExtensions.DefaultLockTimeout;
            if(!Monitor.TryEnter(string.Intern(key), realTimeout))
                SimpleLockItem.TimeoutError(key, realTimeout);

            return new SimpleLockItem(() => Monitor.Exit(string.Intern(key)));
        }
    }
}
