using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Interfaces
{
    public interface IListenable
    {
        public void AddListener(Action func);
        public void RemoveListener(Action func);
        public void Invoke();
    }

    public interface IButtonListenable : IListenable
    {
        public List<Action> OnClick { get; set; }

         void IListenable.AddListener(Action func)
        {
            if (OnClick == null)
            {
                var button = GetButton();

                if (button == null)
                {
                    throw new Exception($"Button can not be found!");
                }

                button.onClick.AddListener(Invoke);

                OnClick = new List<Action>();
            }

            OnClick.Add(func);
        }

        void IListenable.RemoveListener(Action func)
        {
            OnClick.Remove(func);
        }

        void IListenable.Invoke()
        {
            foreach (var action in OnClick)
            {
                action.Invoke();
            }
        }

        public Button GetButton();
    }
}