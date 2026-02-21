using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SunEyeVision.UI.Services.Performance;

namespace SunEyeVision.UI.Services.Performance
{
    /// <summary>
    /// 批量更新作用域 - 暂停集合通知，批量操作完成后发送单个通知
    /// </summary>
    public class BatchUpdateScope : IDisposable
    {
        private readonly ObservableCollection<object> _collection;
        private readonly List<NotifyCollectionChangedEventArgs> _pendingEvents;
        private readonly Action<NotifyCollectionChangedEventArgs>? _onComplete;
        private bool _isDisposed;

        public BatchUpdateScope(
            ObservableCollection<object> collection,
            Action<NotifyCollectionChangedEventArgs>? onComplete = null)
        {
            _collection = collection;
            _pendingEvents = new List<NotifyCollectionChangedEventArgs>();
            _onComplete = onComplete;
            _isDisposed = false;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (var evt in _pendingEvents)
            {
                _onComplete?.Invoke(evt);
            }

            _pendingEvents.Clear();
        }

        public void AddEvent(NotifyCollectionChangedEventArgs evt)
        {
            if (!_isDisposed)
            {
                _pendingEvents.Add(evt);
            }
        }
    }

    /// <summary>
    /// 批量更新集合 - 支持批量操作的 ObservableCollection
    /// </summary>
    public class BatchUpdateCollection<T> : ObservableCollection<T>
    {
        private int _batchDepth;
        private readonly List<NotifyCollectionChangedEventArgs> _pendingEvents;
        private readonly object _lockObj;

        public bool IsBatchUpdating => _batchDepth > 0;

        public BatchUpdateCollection()
        {
            _batchDepth = 0;
            _pendingEvents = new List<NotifyCollectionChangedEventArgs>();
            _lockObj = new object();
        }

        public BatchUpdateCollection(IEnumerable<T> collection) : base(collection)
        {
            _batchDepth = 0;
            _pendingEvents = new List<NotifyCollectionChangedEventArgs>();
            _lockObj = new object();
        }

        /// <summary>
        /// 开始批量更新
        /// </summary>
        public IDisposable BeginBatchUpdate()
        {
            lock (_lockObj)
            {
                _batchDepth++;
                return new BatchUpdateDisposable(this);
            }
        }

        /// <summary>
        /// 批量添加多个项目
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                return;

            using (BeginBatchUpdate())
            {
                foreach (var item in items)
                {
                    Add(item);
                }
            }
        }

        /// <summary>
        /// 批量移除多个项目
        /// </summary>
        public void RemoveRange(IEnumerable<T> items)
        {
            if (items == null)
                return;

            using (BeginBatchUpdate())
            {
                foreach (var item in items)
                {
                    Remove(item);
                }
            }
        }

        /// <summary>
        /// 批量替换所有项目
        /// </summary>
        public void ReplaceAll(IEnumerable<T> items)
        {
            if (items == null)
                return;

            using (BeginBatchUpdate())
            {
                Clear();
                foreach (var item in items)
                {
                    Add(item);
                }
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            lock (_lockObj)
            {
                if (_batchDepth > 0)
                {
                    _pendingEvents.Add(e);
                }
                else
                {
                    base.OnCollectionChanged(e);
                }
            }
        }

        internal void EndBatchUpdate()
        {
            lock (_lockObj)
            {
                _batchDepth--;
                if (_batchDepth == 0)
                {
                    foreach (var evt in _pendingEvents)
                    {
                        base.OnCollectionChanged(evt);
                    }
                    _pendingEvents.Clear();
                }
            }
        }

        private class BatchUpdateDisposable : IDisposable
        {
            private readonly BatchUpdateCollection<T> _collection;
            private bool _isDisposed;

            public BatchUpdateDisposable(BatchUpdateCollection<T> collection)
            {
                _collection = collection;
                _isDisposed = false;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    _collection.EndBatchUpdate();
                }
            }
        }
    }

    /// <summary>
    /// 批量更新管理器 - 管理多个集合的批量更新
    /// </summary>
    public class BatchUpdateManager
    {
        private readonly List<BatchUpdateCollection<object>> _collections;
        private int _batchDepth;
        private readonly object _lockObj;

        public bool IsBatchUpdating => _batchDepth > 0;

        public BatchUpdateManager()
        {
            _collections = new List<BatchUpdateCollection<object>>();
            _batchDepth = 0;
            _lockObj = new object();
        }

        /// <summary>
        /// 注册集合
        /// </summary>
        public void RegisterCollection(BatchUpdateCollection<object> collection)
        {
            lock (_lockObj)
            {
                if (!_collections.Contains(collection))
                {
                    _collections.Add(collection);
                }
            }
        }

        /// <summary>
        /// 开始全局批量更新
        /// </summary>
        public IDisposable BeginGlobalBatchUpdate()
        {
            lock (_lockObj)
            {
                _batchDepth++;
                return new GlobalBatchUpdateDisposable(this);
            }
        }

        private void EndGlobalBatchUpdate()
        {
            lock (_lockObj)
            {
                _batchDepth--;
                if (_batchDepth == 0)
                {
                    foreach (var collection in _collections)
                    {
                        if (collection.IsBatchUpdating)
                        {
                            collection.EndBatchUpdate();
                        }
                    }
                }
            }
        }

        private class GlobalBatchUpdateDisposable : IDisposable
        {
            private readonly BatchUpdateManager _manager;
            private bool _isDisposed;

            public GlobalBatchUpdateDisposable(BatchUpdateManager manager)
            {
                _manager = manager;
                _isDisposed = false;
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    _manager.EndGlobalBatchUpdate();
                }
            }
        }
    }
}
