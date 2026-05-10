namespace LabServer.Server.Controllers;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

using LabServer.Server.Hubs;
using LabServer.Server.Models;
using LabServer.Server.Models.Uni;
using LabServer.Server.Service;
using LabServer.Shared.Models.Uni;

public partial class CoursesController : BaseRestController<CourseModel, CourseData>
{
    private readonly LabServer.Server.Data.LabsContext _labsContext;

    public CoursesController(IDBStorage<CourseModel> storage, UserManager<UserModel> userManager, IHubContext<DataHub> dataHub,
        LabServer.Server.Data.LabsContext labsContext) : base(storage, userManager, dataHub)
    {
        _labsContext = labsContext;
    }
}