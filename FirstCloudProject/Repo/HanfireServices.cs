using FirstCloudProject.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstCloudProject.Repo
{
    public class HanfireServices
    {
            IServiceProvider _serviceProvider;
            public HanfireServices(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public void Send()
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                using (ApplicationDbContext ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
                {
                    //var emails = ctx.Users.Select(x => x.Email).ToList();

                    //foreach (var email in emails)
                    //{
                    //    sendEmail(email, "sample body");
                    //}
                }
            }
    }
}
