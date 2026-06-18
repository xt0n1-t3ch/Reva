using System.Collections.Generic;
using Reva.App.Navigation;

namespace Reva.App.Onboarding;

public static class TourScript
{
    public static IReadOnlyList<TourStep> Steps { get; } =
    [
        new TourStep(
            "Welcome to Reva",
            "This quick tour walks you through the workspace, from ingesting a document to exporting reconciled data. Use Next and Back to move, or Skip to leave at any time.",
            AppRoutes.Dashboard,
            TourTargetNames.ContentHost,
            TourPlacement.Center),

        new TourStep(
            "Dashboard at a glance",
            "The cards at the top summarize the workspace: total documents, what still needs review, open exceptions, and the average extraction confidence. They update as you process work.",
            AppRoutes.Dashboard,
            TourTargetNames.ContentHost,
            TourPlacement.Left),

        new TourStep(
            "Bring in documents",
            "Drop bordereaux, statements, and supporting files into the upload zone, or browse from disk. Reva accepts PDF, DOCX, XLSX, CSV, EML, and image formats, then extracts each one automatically.",
            AppRoutes.Dashboard,
            TourTargetNames.ContentHost,
            TourPlacement.Left),

        new TourStep(
            "Your work queue",
            "Every ingested document lands in the queue with its type, status, confidence, and exception count. Select a row to open it for review.",
            AppRoutes.Dashboard,
            TourTargetNames.ContentHost,
            TourPlacement.Left),

        new TourStep(
            "Move to Review",
            "The Review workspace is where you confirm each extracted field against the source. Reva opens it here so you can see citations side by side with the page.",
            AppRoutes.Review,
            TourTargetNames.NavReview,
            TourPlacement.Right),

        new TourStep(
            "Citations on the page",
            "In the Review workspace, hover a canonical field and Reva highlights exactly where the value was read from on the source page. Every number is traceable back to the document.",
            AppRoutes.Review,
            TourTargetNames.ContentHost,
            TourPlacement.Left),

        new TourStep(
            "Reconciliation checks",
            "Reva re-computes stated totals and flags any that disagree beyond your tolerance, with the detected value, the expected value, and the delta. Approve, request a fix, or reject from the panel on the right.",
            AppRoutes.Review,
            TourTargetNames.ContentHost,
            TourPlacement.Left),

        new TourStep(
            "Learned mappings",
            "As you review, Reva remembers how each sender's headers map to canonical reinsurance fields. Override any mapping here and it applies to that sender going forward.",
            AppRoutes.Mappings,
            TourTargetNames.NavMappings,
            TourPlacement.Right),

        new TourStep(
            "Export templates",
            "Shape the output: pick a template, arrange columns, and preview the rendered file against a real document before you export. Duplicate a built-in template to customize it.",
            AppRoutes.Export,
            TourTargetNames.NavExport,
            TourPlacement.Right),

        new TourStep(
            "Choose your model",
            "Settings is where you pick the local vision-language model Reva uses to read scanned pages, tune reconciliation tolerance and confidence tiers, and brand the workspace.",
            AppRoutes.Settings,
            TourTargetNames.NavSettings,
            TourPlacement.Right),

        new TourStep(
            "Ask the Copilot",
            "The Copilot dock answers questions about your documents and can run actions on your behalf. Open it any time from the header to get unstuck.",
            AppRoutes.Dashboard,
            TourTargetNames.CopilotDock,
            TourPlacement.Left,
            RequiresCopilotOpen: true),

        new TourStep(
            "You are ready",
            "That is the full loop: ingest, review with citations, reconcile, map, and export. Relaunch this tour from the Tour button in the header whenever you need it.",
            AppRoutes.Dashboard,
            TourTargetNames.TourButton,
            TourPlacement.Below)
    ];
}
