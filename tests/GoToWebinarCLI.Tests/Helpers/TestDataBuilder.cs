using GoToWebinarCLI.Models;

namespace GoToWebinarCLI.Tests.Helpers;

public static class TestDataBuilder
{
    public static List<Webinar> CreateWebinars(int count = 3)
    {
        var webinars = new List<Webinar>();
        var baseDate = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            webinars.Add(new Webinar
            {
                WebinarKey = $"webinar-{i + 1}",
                WebinarId = $"id-{i + 1}",
                Subject = $"Test Webinar {i + 1}",
                Description = $"Description for webinar {i + 1}",
                TimeZone = "America/New_York",
                InSession = false,
                NumberOfRegistrants = i * 10,
                RegistrationUrl = $"https://register.gotowebinar.com/register/{i + 1}",
                Times = new List<WebinarTime>
                {
                    new WebinarTime
                    {
                        StartTime = baseDate.AddDays(i + 1),
                        EndTime = baseDate.AddDays(i + 1).AddHours(1)
                    }
                }
            });
        }

        return webinars;
    }

    public static List<Webinar> CreatePastWebinars(int count = 2)
    {
        var webinars = new List<Webinar>();
        var baseDate = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            webinars.Add(new Webinar
            {
                WebinarKey = $"past-webinar-{i + 1}",
                WebinarId = $"past-id-{i + 1}",
                Subject = $"Past Webinar {i + 1}",
                Description = $"Description for past webinar {i + 1}",
                TimeZone = "America/New_York",
                InSession = false,
                NumberOfRegistrants = 25 + (i * 5),
                Times = new List<WebinarTime>
                {
                    new WebinarTime
                    {
                        StartTime = baseDate.AddDays(-(i + 1)),
                        EndTime = baseDate.AddDays(-(i + 1)).AddHours(1)
                    }
                }
            });
        }

        return webinars;
    }

    public static Webinar CreateWebinar(string key = "test-webinar-1")
    {
        return new Webinar
        {
            WebinarKey = key,
            WebinarId = "webinar-id-123",
            Subject = "Advanced Testing Strategies",
            Description = "Learn about advanced testing strategies for .NET applications",
            TimeZone = "America/New_York",
            InSession = false,
            NumberOfRegistrants = 42,
            RegistrationUrl = $"https://register.gotowebinar.com/register/{key}",
            RegistrationLimit = 100,
            Times = new List<WebinarTime>
            {
                new WebinarTime
                {
                    StartTime = DateTime.UtcNow.AddDays(7),
                    EndTime = DateTime.UtcNow.AddDays(7).AddHours(2)
                }
            }
        };
    }

    public static List<Registrant> CreateRegistrants(int count = 5)
    {
        var registrants = new List<Registrant>();

        for (int i = 0; i < count; i++)
        {
            registrants.Add(new Registrant
            {
                RegistrantKey = $"registrant-{i + 1}",
                FirstName = $"John{i + 1}",
                LastName = $"Doe{i + 1}",
                Email = $"john.doe{i + 1}@example.com",
                RegistrationDate = DateTime.UtcNow.AddDays(-i),
                Status = i % 2 == 0 ? "APPROVED" : "PENDING",
                JoinUrl = $"https://global.gotowebinar.com/join/{i + 1}",
                Organization = $"Company {i + 1}",
                Phone = $"555-000{i + 1}"
            });
        }

        return registrants;
    }

    public static Registrant CreateRegistrant(string key = "test-registrant-1")
    {
        return new Registrant
        {
            RegistrantKey = key,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            RegistrationDate = DateTime.UtcNow.AddHours(-1),
            Status = "APPROVED",
            JoinUrl = $"https://global.gotowebinar.com/join/{key}",
            Organization = "Tech Corp",
            Phone = "555-1234",
            JobTitle = "Software Engineer",
            Responses = new List<QuestionResponse>
            {
                new QuestionResponse
                {
                    QuestionKey = "q1",
                    Question = "What is your experience level?",
                    Answer = "Intermediate"
                }
            }
        };
    }
}