using Roadkill.Core.Text;
using Roadkill.Core.Text.Menu;
using Roadkill.Core.Text.TextMiddleware;

namespace Roadkill.Tests.Unit.Text
{
    public class MiddleWareMock : Middleware
    {
        public string SearchString { get; set; }
        public string Replacement { get; set; }

        public override PageHtml Invoke(PageHtml pageHtml)
        {
            return pageHtml.Html.Replace(SearchString, Replacement);
        }
    }
}