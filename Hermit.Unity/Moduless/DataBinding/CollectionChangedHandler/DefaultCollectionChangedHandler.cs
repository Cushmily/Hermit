using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Hermit.DataBinding;
using Hermit.View;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hermit
{
    public class DefaultCollectionChangedHandler : IViewCollectionChangedHandler
    {
        protected GameObject ViewTemplate;

        protected Transform ViewContainer;

        protected IViewManager ViewManager { get; }

        protected Dictionary<object, GameObject> instantiatedGameObjects = new Dictionary<object, GameObject>();

        public DefaultCollectionChangedHandler()
        {
            ViewManager = Her.Resolve<IViewManager>();
        }

        public void SetUp(GameObject viewTemplate, Transform viewContainer)
        {
            ViewTemplate = viewTemplate;
            ViewContainer = viewContainer;
        }

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (var i = 0; i < e.NewItems.Count; i++)
                    {
                        var eNewItem = e.NewItems[i];
                        var instance = Object.Instantiate(ViewTemplate, ViewContainer, false);

                        // Add to caches
                        instantiatedGameObjects.Add(eNewItem, instance);

                        // View
                        var viewBase = instance.GetComponent<IView>();
                        viewBase?.SetUpViewInfo();

                        // View Model Inject
                        var dataProvider = instance.GetComponent<IViewModelProvider>();
                        dataProvider?.SetViewModel(eNewItem);

                        instance.gameObject.SetActive(true);
                        instance.transform.SetSiblingIndex(e.NewStartingIndex + i);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:

                    for (var i = 0; i < e.OldItems.Count; i++)
                    {
                        var index = e.OldItems[i];
                        if (!instantiatedGameObjects.TryGetValue(index, out var instance)) { throw new Exception($"Index: {index} has no founded instances.");}

                        instantiatedGameObjects.Remove(index);

                        // View
                        var viewBase = instance.GetComponent<IView>();
                        viewBase?.CleanUpViewInfo();

                        Object.Destroy(instance.gameObject);
                    }

                    break;
                case NotifyCollectionChangedAction.Move:
                    var moveTarget = ViewContainer.GetChild(e.OldStartingIndex);
                    moveTarget.transform.SetSiblingIndex(e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (var i = 0; i < e.NewItems.Count; i++)
                    {
                        var replaceTarget = ViewContainer.GetChild(e.OldStartingIndex + i);
                        var dataProvider = replaceTarget.GetComponent<IViewModelProvider>();
                        var newItem = e.NewItems[i];

                        dataProvider.SetViewModel(newItem);
                        dataProvider.ReBindAll();
                    }

                    break;
                case NotifyCollectionChangedAction.Reset:
                    var childCount = ViewContainer.childCount;
                    for (var i = childCount - 1; i >= 0; i--) { Object.Destroy(ViewContainer.GetChild(i).gameObject); }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}