using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    // Change this line:
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        // Your dependencies go here
    })
    .Build();

host.Run();