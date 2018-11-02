using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// Holds TantaMFTAsyncMessageHolder events.  Dequeue also blocks (via semaphore)
    /// if no event is ready.
    /// 
    /// Multiple processing threads can be trying to access this class
    /// while a the same time a ProcessInput message could be trying
    /// to add something.  Rather than have the processing threads call
    /// dequeue in a loop, they wait on a counting semaphore which gets released
    /// in Enqueue.
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    public class TantaMFTAsyncBlockingQueue
    {
        // A ConcurrentQueue represents a thread-safe first in-first out (FIFO) collection.
        protected readonly ConcurrentQueue<TantaMFTAsyncMessageHolder> m_queue;
        protected readonly SemaphoreSlim m_sem;
        protected int m_InputsEnqueued;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public TantaMFTAsyncBlockingQueue()
        {
            m_queue = new ConcurrentQueue<TantaMFTAsyncMessageHolder>();
            m_sem = new SemaphoreSlim(0, int.MaxValue);
            m_InputsEnqueued = 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Enqueues a message. 
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public void Enqueue(TantaMFTAsyncMessageHolder item)
        {
            lock (m_queue)
            {
                item.InputNumber = m_InputsEnqueued;

                m_InputsEnqueued++;

                m_queue.Enqueue(item);
            }
            m_sem.Release();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Dequeues a message. 
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public TantaMFTAsyncMessageHolder Dequeue()
        {
            // block if other threads are using the semaphore
            m_sem.Wait();

            TantaMFTAsyncMessageHolder mh;
            while (m_queue.TryDequeue(out mh)==false)
                ;

            return mh;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles the shutdown
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public void Shutdown()
        {
            TantaMFTAsyncMessageHolder mh;
            while (m_queue.IsEmpty == false)
            {
                m_queue.TryDequeue(out mh);
            }

            m_sem.Dispose();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Counts the number of enqueued messages
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public int Count
        {
            get { return m_queue.Count; }
        }
    }
}
