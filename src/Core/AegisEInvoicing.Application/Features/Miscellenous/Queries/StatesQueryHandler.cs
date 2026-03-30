using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.DTOs;
using AegisEInvoicing.Application.Features.Miscellenous.DTOs;
using MediatR;
using Newtonsoft.Json;

namespace AegisEInvoicing.Application.Features.Miscellenous.Queries;

public class StatesQueryHandler : IRequestHandler<StatesQuery, PaginatedList<StatesSummaryDto>>
{    

    public async Task<PaginatedList<StatesSummaryDto>> Handle(StatesQuery request, CancellationToken cancellationToken)
    {

        var states = await GetStates();

        return new PaginatedList<StatesSummaryDto>(states, states.Count, 1, 1);
    }

    private async Task<List<StatesSummaryDto>> GetStates()
    {
        List<StatesSummaryDto>? stateSummaryDtos = new List<StatesSummaryDto>();
        string currentDirectory = Directory.GetCurrentDirectory();
        string completeFilePath = currentDirectory + "\\states-and-cities.json";
        string jsonString = File.ReadAllText(completeFilePath);
        var state_cities = JsonConvert.DeserializeObject<List<StateCityDto>>(jsonString);        

        stateSummaryDtos = state_cities?.Select(x => new StatesSummaryDto()
        {
            Name = x.name
        }).ToList();

        return await Task.FromResult(stateSummaryDtos ?? new List<StatesSummaryDto>());
    }
}
