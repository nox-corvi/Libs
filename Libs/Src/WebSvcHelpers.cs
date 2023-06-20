namespace Nox
{
#if NET
    public enum CacheExpirationEnum
    {
        NoExpiration,
        AbsoluteExpiration,
        SlidingExpiration,
    }

//    public class WebSvcHelpers
//    {
//        public static HttpContext context = HttpContext.Current;

//        public static string SerializeException(Exception ex)
//        {
//            var Result = new StringBuilder();

//            if (ex != null)
//            {
//                Result.Append($"<exception>");
//                Result.Append($"<source>{ ex.Source }</source>");
//                Result.Append($"<message>{ ex.Message }</message>");
//                Result.Append($"<stacktrace>{ ex.StackTrace }</stacktrace>");
//                Result.Append($"<data>");
//                foreach (var Item in ex.Data.Keys)
//                    Result.Append($"<{Item}>{ex.Data[Item].ToString()}</{Item}>");

//                if (ex.InnerException != null)
//                    Result.Append(SerializeException(ex.InnerException));

//                Result.Append($"</data>");

//                Result.Append($"</exception>");
//            }
//            else
//                Result.Append($"<exception />");

//            return Result.ToString();
//        }

//#region Cache Methods
//        public static uint HashValues(params string[] Values)
//        {
//            var SB = new StringBuilder(AppDomain.CurrentDomain.FriendlyName);
//            foreach (var Item in Values)
//                SB.AppendLine(Item);

//            return UniLib.Hash.HashFNV1a32(SB.ToString());
//        }

//        public static int CacheExpirationTime() => CacheValue<int>(nameof(CacheExpirationTime),
//            CacheNotHit: () => int.Parse(UniLib.Helpers.NZ(ConfigurationManager.AppSettings[nameof(CacheExpirationTime)], "5")));

//        public static T CacheValue<T>(string Key, Func<T> CacheNotHit) where T : struct
//        {
//            var cache = context.Cache;
//            //var expiration = DateTime.Now.AddMinutes(CacheExpirationTime());

//            try
//            {
//                object Value = context.Cache[Key];
//                if (Value == null)
//                {
//                    Log.LogMessage($"Add to Cache {Key}:{Helpers.NZ(Value)}", Log.LogLevelEnum.Trace);

//                    var CacheValue = CacheNotHit.Invoke();
//                    cache.Insert(Key, CacheValue);

//                    return CacheValue;
//                }
//                else
//                {
//                    Log.LogMessage($"Get from Cache {Key}:{Helpers.NZ(Value)}", Log.LogLevelEnum.Trace);
//                    return UniLib.Helpers.To<T>(Value?.ToString());
//                }
//            }
//            catch (Exception ex)
//            {
//                Log.LogException(ex);
//                return default(T);
//            }
//        }

//        public static T SetCacheValue<T>(string Key, Func<T> ValueFunc) where T : struct
//        {
//            var cache = context.Cache;

//            try
//            {
//                var CacheValue = ValueFunc.Invoke();
//                cache.Insert(Key, CacheValue);

//                Log.LogMessage($"Update to Cache {Key}:{Helpers.NZ(CacheValue)}", Log.LogLevelEnum.Trace);

//                return CacheValue;
//            }
//            catch (Exception ex)
//            {
//                Log.LogException(ex);
//                return default(T);
//            }
//        }

//        public static bool CacheValueExists(string Key)
//        {
//            var cache = context.Cache;
//            //var expiration = DateTime.Now.AddMinutes(CacheExpirationTime());

//            try
//            {
//                object Value = context.Cache[Key];
//                return (Value != null);
//            }
//            catch (Exception ex)
//            {
//                Log.LogException(ex);
//                return false;
//            }
//        }

//        public static T GetCacheValue<T>(string Key, T DefaultValue = default(T)) where T : struct
//        {
//            var cache = context.Cache;
//            //var expiration = DateTime.Now.AddMinutes(CacheExpirationTime());

//            try
//            {
//                object Value = context.Cache[Key];
//                if (Value == null)
//                    return DefaultValue;
//                else
//                {
//                    Log.LogMessage($"Get from Cache {Key}:{Helpers.NZ(Value)}", Log.LogLevelEnum.Trace);
//                    return UniLib.Helpers.To<T>(Value?.ToString());
//                }
//            }
//            catch (Exception ex)
//            {
//                Log.LogException(ex);
//                return default(T);
//            }
//        }

//        public static T CacheObj<T>(string Key, Func<T> CacheNotHit) where T : class =>
//            CacheObj(Key, CacheNotHit, CacheExpirationEnum.AbsoluteExpiration, CacheItemPriority.Default, null);

//        public static T CacheObj<T>(string Key, Func<T> CacheNotHit, CacheExpirationEnum Expiration, CacheItemPriority Priority, CacheItemRemovedCallback cacheItemRemovedCallback) where T : class
//        {
//            var cache = context.Cache;

//            try
//            {
//                object Value = context.Cache[Key];
//                if (Value == null)
//                {
//                    Log.LogMessage($"Add to Cache {Key}:<{typeof(T).Name}>", Log.LogLevelEnum.Trace);

//                    var CacheObject = CacheNotHit.Invoke();

//                    switch (Expiration)
//                    {
//                        case CacheExpirationEnum.NoExpiration:
//                            cache.Insert(Key, CacheObject, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, Priority,
//                                (string key, object value, CacheItemRemovedReason reason) =>
//                                {
//                                    cacheItemRemovedCallback?.Invoke(key, value, reason);
//                                    Log.LogMessage($"Removed from Cache {key}", Log.LogLevelEnum.Trace);
//                                });
//                            break;
//                        case CacheExpirationEnum.AbsoluteExpiration:
//                            cache.Insert(Key, CacheObject, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, WebSvcHelpers.CacheExpirationTime(), 0), Priority,
//                                (string key, object value, CacheItemRemovedReason reason) =>
//                                {
//                                    cacheItemRemovedCallback?.Invoke(key, value, reason);
//                                    Log.LogMessage($"Removed from Cache {key}", Log.LogLevelEnum.Trace);
//                                });
//                            break;
//                        case CacheExpirationEnum.SlidingExpiration:

//                            cache.Insert(Key, CacheObject, null, DateTime.Now.AddMinutes(WebSvcHelpers.CacheExpirationTime()), Cache.NoSlidingExpiration, Priority,
//                                (string key, object value, CacheItemRemovedReason reason) =>
//                                {
//                                    cacheItemRemovedCallback?.Invoke(key, value, reason);
//                                    Log.LogMessage($"Removed from Cache {key}", Log.LogLevelEnum.Trace);
//                                });
//                            break;
//                    }
//                    return CacheObject;
//                }
//                else
//                {
//                    Log.LogMessage($"Get from Cache {Key}:<{typeof(T).Name}>", Log.LogLevelEnum.Trace);
//                    return (T)Value;
//                }
//            }
//            catch (Exception ex)
//            {
//                Log.LogException(ex);
//                return null;
//            }
//        }

//        public static T RemoveCacheObj<T>(string Key) where T : class
//        {
//            var cache = context.Cache;

//            try
//            {
//                return (T)context.Cache.Remove(Key);
//            }
//            catch (Exception ex)
//            {
//                Log.LogException(ex);
//                return null;
//            }
//        }
//#endregion
//    }
#endif
}
