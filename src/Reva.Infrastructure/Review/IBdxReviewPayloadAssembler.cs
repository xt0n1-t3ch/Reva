using Reva.Core.Contracts;
using Reva.Infrastructure.Persistence;

namespace Reva.Infrastructure.Review;

public interface IBdxReviewPayloadAssembler
{
    BdxReviewPayload Assemble(DocumentRecord document);
}
