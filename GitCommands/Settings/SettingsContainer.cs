using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitUIPluginInterfaces;

namespace GitCommands.Settings
{
    public class SettingsContainer<L> : ISettingsSource where L : SettingsContainer<L>
    {
        public L LowerPriority { get; private set; }
        public SettingsCache SettingsCache { get; private set; }

        public SettingsContainer(L aLowerPriority, SettingsCache aSettingsCache)
        {
            LowerPriority = aLowerPriority;
            SettingsCache = aSettingsCache;
        }

        public void LockedAction(Action action)
        {
            SettingsCache.LockedAction(() =>
                {
                    if (LowerPriority != null)
                    {
                        LowerPriority.LockedAction(action);
                    }
                    else
                    {
                        action();
                    }
                });
        }

        public void Save()
        {
            SettingsCache.Save();

            if (LowerPriority != null)
            {
                LowerPriority.Save();
            }
        }

        public T GetValue<T>(string name, T defaultValue, Func<string, T> decode)
        {
            return GetValue<T>(name, defaultValue, decode, null);
        }

        public T GetValue<T>(string name, T defaultValue, Func<string, T> decode, Func<T, T, T> merge)
        {
            T value;

            TryGetValue(name, defaultValue, decode, merge, out value);

            return value;
        }

        /// <summary>
        /// sets given value at the possible lowest priority level
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="encode"></param>
        public virtual void SetValue<T>(string name, T value, Func<T, string> encode)
        {
            if (LowerPriority == null || SettingsCache.HasValue(name))
                SettingsCache.SetValue(name, value, encode);
            else
                LowerPriority.SetValue(name, value, encode);
        }

        public virtual bool TryGetValue<T>(string name, T defaultValue, Func<string, T> decode, Func<T, T, T> merge, out T value)
        {
            bool isSetHere = SettingsCache.TryGetValue<T>(name, defaultValue, decode, out value);

            if (isSetHere && merge == null)
                return true;

            T lowerPrioValue;

            if (LowerPriority != null && LowerPriority.TryGetValue(name, defaultValue, decode, merge, out lowerPrioValue))
            {
                if (isSetHere && merge != null)
                {
                    value = merge(lowerPrioValue, value);
                }
                else
                    value = lowerPrioValue;

                return true;
            }

            return false;
        }

        public bool SetValueHere<T>( string name, T value, Func<T, string> encode )
        {
            SettingsCache.SetValue<T>( name, value, encode );
            return true;
        }

        public bool GetValueHere<T>( string name, T defaultValue, Func<string, T> decode, out T value )
        {
            return SettingsCache.TryGetValue<T>( name, defaultValue, decode, out value );
        }

        public bool GetValueHereWithMerge<T>( string name, T defaultValue, Func<string, T> decode, Func<T, T, T> merge, out T value )
        {
            value = defaultValue;

            if( LowerPriority == null )
                return GetValueHere<T>( name, defaultValue, decode, out value );
            else
            {
                T highValue;
                if( !GetValueHere<T>( name, defaultValue, decode, out highValue ) )
                    return false;

                T lowValue;
                if( !LowerPriority.GetValueHereWithMerge<T>( name, defaultValue, decode, merge, out lowValue ) )
                    return false;

                value = merge( lowValue, highValue );
                return true;
            }
        }
    }
}
