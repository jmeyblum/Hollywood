using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace Hollywood.Runtime.Internal
{
	public class Hierarchy<T> where T : class
	{
		private Dictionary<T, HashSet<T>> ParentToChildren = new Dictionary<T, HashSet<T>>();
		private Dictionary<T, T> ChildToParent = new Dictionary<T, T>();

		public bool Locked { get; set; }

		public void Add(T element, T parent = null)
		{
			if (Locked)
			{
				return;
			}

			Assert.IsFalse(Contains(element));
			Assert.IsTrue(parent == null || Contains(parent));

			ChildToParent.Add(element, parent ?? element);

			if (parent != null)
			{
				if (!ParentToChildren.TryGetValue(parent, out HashSet<T> parentChildren))
				{
					parentChildren = new HashSet<T>();
					ParentToChildren.Add(parent, parentChildren);
				}

				parentChildren.Add(element);
			}
		}

		public void Remove(T element, bool recursively = false)
		{
			if (Locked)
			{
				return;
			}

			Assert.IsTrue(Contains(element));
			Assert.IsTrue(!recursively || GetChildren(element).Count() == 0);

			if (recursively)
			{
				if (ParentToChildren.TryGetValue(element, out HashSet<T> elementChildren))
				{
					// Can't use foreach since Remove modifies elementChildren internally
					while (elementChildren.Count > 0)
					{
						var child = elementChildren.First();
						Remove(child, recursively);
					}
				}
			}

			T parent = ChildToParent[element];
			if (parent != element && ParentToChildren.TryGetValue(parent, out HashSet<T> parentChildren))
			{
				parentChildren.Remove(element);
			}
			ChildToParent.Remove(element);
		}

		public IEnumerable<T> GetChildren(T parent)
		{
			Assert.IsTrue(Contains(parent));

			if (!ParentToChildren.TryGetValue(parent, out var children))
			{
				return Enumerable.Empty<T>();
			}

			return children;
		}

		public T GetParent(T child)
		{
			Assert.IsTrue(Contains(child));

			T parent = ChildToParent[child];

			return parent != child ? parent : null;
		}

		public bool Contains(T element)
		{
			Assert.IsNotNull(element);

			return ChildToParent.ContainsKey(element);
		}
	}
}