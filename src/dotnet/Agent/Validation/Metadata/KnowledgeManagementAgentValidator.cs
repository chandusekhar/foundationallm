using FluentValidation;
using FoundationaLLM.Common.Models.Agents;

namespace FoundationaLLM.Agent.Validation.Metadata
{
    /// <summary>
    /// Validator for the <see cref="KnowledgeManagementAgent"/> model.
    /// </summary>
    public class KnowledgeManagementAgentValidator : AbstractValidator<KnowledgeManagementAgent>
    {
        /// <summary>
        /// Configures the validation rules for the <see cref="KnowledgeManagementAgent"/> model.
        /// </summary>
        public KnowledgeManagementAgentValidator()
        {
            Include(new AgentBaseValidator());

            RuleFor(x => x.IndexingProfile).NotEmpty().WithMessage("Indexing profile is required for Knowledge Management Agents.");
            RuleFor(x => x.EmbeddingProfile).NotEmpty().WithMessage("Embedding profile is required for Knowledge Management Agents.");
        }
    }
}
