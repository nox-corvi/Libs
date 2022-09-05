using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Nox.Net
{
    public class DataController<C, T, U> : ControllerBase where C: DbContext where T : class where U : struct
    {
        private C _Context = null!;
        private ILogger<T> _Logger = null!;

        #region Properties
        public C Context =>
            _Context;

        public ILogger<T> Logger =>
            _Logger;
        #endregion

        [HttpGet]
        public IActionResult GetWhereId(U Id)
        {
            var item = _Context.Set<T>().Find(Id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        protected IActionResult ModifyRecord(
            Func<IActionResult> CreateInstance,
            Func<T, IActionResult> Map,
            Func<T, IActionResult> ActionData,
            Func<T, IActionResult> MapDatabase)
        {
            IActionResult r = null!;
            Func<T> row = () => ((r ?? throw new ArgumentNullException()) as ObjectResult).Value as T;

            try
            {
                r = CreateInstance.Invoke();
                if (r is OkObjectResult)
                {
                    r = Map(row.Invoke());
                    if (r is OkObjectResult)
                    {
                        r = ActionData.Invoke(row.Invoke());
                        if (r is OkObjectResult)
                        {
                            r = MapDatabase.Invoke(row.Invoke());
                            if (r is OkObjectResult)
                            {
                                Context.SaveChanges();

                                return Ok(row.Invoke());
                            }
                            else // pass through
                                return r;
                        }
                        else // pass through
                            return r;
                    }
                    else // pass through
                        return r;
                }
                else
                    return r;
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        protected IActionResult DeleteRecord(
            Func<IActionResult> CreateInstance,

            Func<T, IActionResult> Validate,
            Func<T, IActionResult> Execute)
        {
            IActionResult r = null!;
            Func<T> row = () => ((r ?? throw new ArgumentNullException()) as ObjectResult).Value as T;

            try
            {
                r = CreateInstance.Invoke();
                if (r is OkObjectResult)
                {
                    r = Validate(row.Invoke());

                    if (r is OkObjectResult)
                    {
                        r = Execute((T)r);
                        if (r is OkObjectResult)
                        {
                            Context.SaveChanges();

                            return NoContent();
                        }
                        else
                            return r;
                    }
                    else
                        return r;
                }
                else
                    return r;
            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }

        public DataController(C context, ILogger<T> logger)
                    : base()
        {
            _Context = context;
            _Logger = logger;
        }
    }

}
