using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Application.DTOs;
using Play.Application.IRepository;
using System.Diagnostics;

namespace Play.APIs.Controllers.v1;

[Route("api/products")]
[ApiController]
[Authorize]
public class ProductsController : ControllerBase
{

}
