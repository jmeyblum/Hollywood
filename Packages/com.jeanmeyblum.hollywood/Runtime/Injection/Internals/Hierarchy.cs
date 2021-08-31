using System.Collections.Generic;
using System.Linq;

namespace Hollywood.Internal
{
	public class Hierarchy<T> where T : class, new()
	{
		public readonly T Root = new T();

		private Dictionary<T, HashSet<T>> ParentToChildren = new Dictionary<T, HashSet<T>>();
		private Dictionary<T, T> ChildToParent = new Dictionary<T, T>();

		public Hierarchy()
		{
			ChildToParent[Root] = Root;
		}

		public void Add(T element, T parent = null)
		{
			parent ??= Root;

			if (!Contains(parent))
			{
				Add(parent);
			}

			Assert.IsTrue(!Contains(element) || GetParent(element) == Root, $"{element} is already parented.");

			if (Contains(element) && GetParent(element) == Root)
			{
				ChildToParent.Remove(element);
				ParentToChildren[Root].Remove(element);
			}

			ChildToParent[element] = parent;

			if (!ParentToChildren.TryGetValue(parent, out HashSet<T> parentChildren))
			{
				parentChildren = new HashSet<T>();
				ParentToChildren.Add(parent, parentChildren);
			}

			parentChildren.Add(element);
		}

		public void Remove(T element, bool recursively = false)
		{
			Assert.IsNotNull(element, $"{nameof(element)} is null.");
			Assert.IsTrue(Contains(element), $"{element} is unknown from this {nameof(Hierarchy<T>)}: {this}");
			Assert.IsTrue(!recursively || GetChildren(element).Count() == 0, $"Can't remove {nameof(element)} with children. Set {nameof(recursively)} to true to do so.");

			if (recursively)
			{
				if (ParentToChildren.TryGetValue(element, out HashSet<T> elementChildren))
				{
					// Can't use foreach since Remove modifies elementChildren internally
					while (elementChildren.Any())
					{
						var child = elementChildren.First();
						Remove(child, recursively);
					}
				}
			}
			ParentToChildren.Remove(element);

			T parent = ChildToParent[element];
			if (parent != element && ParentToChildren.TryGetValue(parent, out HashSet<T> parentChildren))
			{
				parentChildren.Remove(element);
			}
			ChildToParent.Remove(element);
		}

		public IEnumerable<T> GetChildren(T parent)
		{
			if (!Contains(parent) || !ParentToChildren.TryGetValue(parent, out var children))
			{
				return Enumerable.Empty<T>();
			}

			return children;
		}

		public T GetParent(T child)
		{
			Assert.IsTrue(Contains(child), $"{child} is unknown from this {nameof(Hierarchy<T>)}: {this}");

			T parent = ChildToParent[child];

			return parent != child ? parent : null;
		}

		public bool Contains(T element)
		{
			Assert.IsNotNull(element, $"{nameof(element)} is null.");

			return ChildToParent.ContainsKey(element);
		}

		public void Reset()
		{
			ParentToChildren.Clear();
			ChildToParent.Clear();
		}
	}
}