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
		/// <param name="maxSize">A limit for the pool capacity. Enforcement of this limit is not thread-synchronized, so it may be exceeded slightly if objects are put in the pool from multiple threads.</param>
		/// <param name="automaticallyCalledWhenPoolingObject">The ObjectPool will call this method for each object that is about to be added to the pool. Usually, it is fine to let this be null. If you are putting something like a List in the pool, you may want this method to call List.Clear().</param>
		public ObjectPool(Func<T> objectGenerator, int maxSize = int.MaxValue, Action<T> automaticallyCalledWhenPoolingObject = null)
		{
			if (objectGenerator == null)
				throw new ArgumentNullException("objectGenerator");
			_objects = new ConcurrentQueue<T>();
			_objectGenerator = objectGenerator;
			_maxSize = maxSize;
			_automaticallyCalledWhenPoolingObject = automaticallyCalledWhenPoolingObject;
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
			if (item != null && _objects.Count < _maxSize)
			{
				if (_automaticallyCalledWhenPoolingObject != null)
					_automaticallyCalledWhenPoolingObject(item);
				if (_objects.Count < _maxSize)
					_objects.Enqueue(item);
			}
		}
	}
}
