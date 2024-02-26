using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A thread-safe pool where objects of a specific type can be stored and retrieved for later use. Pooling often-used or expensive-to-create objects can improve application performance at the cost of higher memory usage.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObjectPool<T>
	{
		private ConcurrentQueue<T> _objects; // Testing demonstrates ConcurrentQueue is much faster than ConcurrentBag
		private Func<T> _objectGenerator;
		private Action<T> _automaticallyCalledWhenPoolingObject;
		private Action<T> _onDestroy;
		private int _maxSize;
		public int Count
		{
			get
			{
				return _objects.Count;
			}
		}

		/// <summary>
		/// Creates an object pool.
		/// </summary>
		/// <param name="objectGenerator">A method which generates new objects of the desired type.</param>
		/// <param name="maxSize">Max number of items the pool should be able to hold. Enforcement of this limit is not thread-synchronized, so it may be exceeded slightly if objects are put in the pool from multiple threads. When the pool is at capacity, the <see cref="PutObject(T)"/> method will destroy the item. See also the [onDestroy] argument.</param>
		/// <param name="automaticallyCalledWhenPoolingObject">The ObjectPool will call this method for each object that is about to be added to the pool. Usually, it is fine to let this be null. If you are putting something like a List in the pool, you may want this method to call List.Clear().</param>
		/// <param name="onDestroy">When the pool is full, items that cannot be put into the pool will be passed to this action.  If onDestroy is null, the item's Dispose method will be called if the type implements IDisposable.</param>
		public ObjectPool(Func<T> objectGenerator, int maxSize = int.MaxValue, Action<T> automaticallyCalledWhenPoolingObject = null, Action<T> onDestroy = null)
		{
			if (objectGenerator == null)
				throw new ArgumentNullException("objectGenerator");
			_objects = new ConcurrentQueue<T>();
			_objectGenerator = objectGenerator;
			_maxSize = maxSize;
			_automaticallyCalledWhenPoolingObject = automaticallyCalledWhenPoolingObject;
			_onDestroy = onDestroy;
		}

		/// <summary>
		/// Gets an object from the pool, or constructs a new one if none are available. You should assume all fields of the object contain garbage data.
		/// </summary>
		/// <returns></returns>
		public T GetObject(Func<T> overrideObjectGenerator = null)
		{
			T item;
			if (_objects.TryDequeue(out item))
				return item;
			return (overrideObjectGenerator ?? _objectGenerator)();
		}

		/// <summary>
		/// Returns an object to the pool so it may be obtained by a later call to GetObject(). Do not use the object after passing it to this method (let it go out of scope).
		/// </summary>
		/// <remarks>
		/// If the pool is currently at or above its max size, the item will not be pooled.
		/// </remarks>
		/// <param name="item"></param>
		public void PutObject(T item)
		{
			// This size check and Add is not thread-synchronized, as speed is more important than strictly honoring the size limit.
			if (item != null)
			{
				if (_objects.Count < _maxSize)
				{
					if (_automaticallyCalledWhenPoolingObject != null)
						_automaticallyCalledWhenPoolingObject(item);
					if (_objects.Count < _maxSize)
						_objects.Enqueue(item);
					else
						NotAddingItem(item);
				}
				else
					NotAddingItem(item);
			}
		}
		/// <summary>
		/// Called when the pool is asked to put this item in the pool, but the item is not being added because the pool is full.
		/// </summary>
		/// <param name="item">Item that is not being pooled.</param>
		private void NotAddingItem(T item)
		{
			if (_onDestroy != null)
				_onDestroy(item);
			else if (typeof(T) is IDisposable)
				(item as IDisposable).Dispose();
		}
	}
}
