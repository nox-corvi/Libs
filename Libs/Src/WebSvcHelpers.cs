﻿//using System;
//using System.Configuration;
//using System.Text;
//using System.Web;
//using System.Web.Caching;
//using Microsoft.Extensions.Logging;
//using Nox;

//namespace Nox
//{
//    public enum CacheExpirationEnum
//    {
//        NoExpiration,
//        AbsoluteExpiration,
//        SlidingExpiration,
//    }

//    public class WebSvcHelpers
//    {
//        public static HttpContext context = HttpContext.;

//        public static string SerializeException(Exception ex)
//        {
//            var Result = new StringBuilder();

//            if (ex != null)
//            {
//                Result.Append($"<exception>");
//                Result.Append($"<source>{ex.Source}</source>");
//                Result.Append($"<message>{ex.Message}</message>");
//                Result.Append($"<stacktrace>{ex.StackTrace}</stacktrace>");
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

//        #region Cache Methods
//        public static uint HashValues(params string[] Values)
//        {
//            var SB = new StringBuilder(AppDomain.CurrentDomain.FriendlyName);
//            foreach (var Item in Values)
//                SB.AppendLine(Item);

//            return Hash.HashFNV1a32(SB.ToString());
//        }

//        public static int CacheExpirationTime() => CacheValue<int>(nameof(CacheExpirationTime),
//            CacheNotHit: () => int.Parse(Nox.Helpers.NZ(ConfigurationManager.AppSettings[nameof(CacheExpirationTime)], "5")));

//        public static T CacheValue<T>(string Key, Func<T> CacheNotHit) where T : struct
//        {
//            var cache = System.Runtime.Caching;
//            var cache1 = context.Cache;
//            //var expiration = DateTime.Now.AddMinutes(CacheExpirationTime());

//            try
//            {
//                object Value = context.Cache[Key];
//                if (Value == null)
//                {
//                    Global.Log.LogMessage($"Add to Cache {Key}:{Helpers.NZ(Value)}", Log4.Log4LevelEnum.Trace);

//                    var CacheValue = CacheNotHit.Invoke();
//                    cache.Insert(Key, CacheValue);

//                    return CacheValue;
//                }
//                else
//                {
//                    Global.Log.LogMessage($"Get from Cache {Key}:{Helpers.NZ(Value)}", Log4.Log4LevelEnum.Trace);
//                    return Nox.Helpers.To<T>(Value?.ToString());
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Log.LogException(ex);
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

//                Global.Log.LogMessage($"Update to Cache {Key}:{Helpers.NZ(CacheValue)}", Log4.Log4LevelEnum.Trace);

//                return CacheValue;
//            }
//            catch (Exception ex)
//            {
//                Global.Log.LogException(ex);
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
//                Global.Log.LogException(ex);
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
//                    Global.Log.LogMessage($"Get from Cache {Key}:{Helpers.NZ(Value)}", Log4.Log4LevelEnum.Trace);
//                    return Helpers.To<T>(Value?.ToString());
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Log.LogException(ex);
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
//                    Global.Log.LogMessage($"Add to Cache {Key}:<{typeof(T).Name}>", Log4.Log4LevelEnum.Trace);

//                    var CacheObject = CacheNotHit.Invoke();

//                    switch (Expiration)
//                    {
//                        case CacheExpirationEnum.NoExpiration:
//                            cache.Insert(Key, CacheObject, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, Priority,
//                                (string key, object value, CacheItemRemovedReason reason) =>
//                                {
//                                    cacheItemRemovedCallback?.Invoke(key, value, reason);
//                                    Global.Log.LogMessage($"Removed from Cache {key}", Log4.Log4LevelEnum.Trace);
//                                });
//                            break;
//                        case CacheExpirationEnum.AbsoluteExpiration:
//                            cache.Insert(Key, CacheObject, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, WebSvcHelpers.CacheExpirationTime(), 0), Priority,
//                                (string key, object value, CacheItemRemovedReason reason) =>
//                                {
//                                    cacheItemRemovedCallback?.Invoke(key, value, reason);
//                                    Global.Log.LogMessage($"Removed from Cache {key}", Log4.Log4LevelEnum.Trace);
//                                });
//                            break;
//                        case CacheExpirationEnum.SlidingExpiration:

//                            cache.Insert(Key, CacheObject, null, DateTime.Now.AddMinutes(WebSvcHelpers.CacheExpirationTime()), Cache.NoSlidingExpiration, Priority,
//                                (string key, object value, CacheItemRemovedReason reason) =>
//                                {
//                                    cacheItemRemovedCallback?.Invoke(key, value, reason);
//                                    Global.Log.LogMessage($"Removed from Cache {key}", Log4.Log4LevelEnum.Trace);
//                                });
//                            break;
//                    }
//                    return CacheObject;
//                }
//                else
//                {
//                    Global.Log.LogMessage($"Get from Cache {Key}:<{typeof(T).Name}>", Log4.Log4LevelEnum.Trace);
//                    return (T)Value;
//                }
//            }
//            catch (Exception ex)
//            {
//                Global.Log.LogException(ex);
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
//                Global.Log.LogException(ex);
//                return null;
//            }
//        }
//        #endregion



//        public WebSvcHelpers(ILogger logger, IHttpcontext)
//    }
//}
