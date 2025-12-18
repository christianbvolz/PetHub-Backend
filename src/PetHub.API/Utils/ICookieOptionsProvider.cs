namespace PetHub.API.Utils;

public interface ICookieOptionsProvider
{
    CookieOptions CreateRefreshCookieOptions();

    CookieOptions CreateDeleteCookieOptions();
}
