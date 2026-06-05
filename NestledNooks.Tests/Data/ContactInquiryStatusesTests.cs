using NestledNooks.Data;

namespace NestledNooks.Tests.Data;

public sealed class ContactInquiryStatusesTests
{
    [Fact]
    public void All_ContainsExpectedWorkflowStatesInOrder()
    {
        Assert.Equal(
            new[] { "New", "Read", "Replied", "Archived" },
            ContactInquiryStatuses.All);

        Assert.DoesNotContain(
            ContactInquiryStatuses.All,
            status => string.IsNullOrWhiteSpace(status));
    }
}
