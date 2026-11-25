namespace LangCenter.API.Pages;

public record PaginationLinks(string PageName, int Page, int PageSize, int TotalPages,
    Guid? ClassId = null, string? Email = null, string? Fragment = null);
