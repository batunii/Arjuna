# Reply to John — 2026-07-30 (post-presentation feedback)

Subject: Re: Presentation takeaways — replies, and yes to Tuesday 1:45

Hi John,

Thank you for writing these up so quickly — it is much more useful having them while the
discussion is still fresh. **Tuesday 4 August at 1:45 suits me, and I would like to take it.**
Replies in your order below.

**1 · Title.** Agreed, and that is how it is already set up: the dissertation title is *A Study
in Adaptive Visual Noise Cancellation for Attention Guidance on Consumer Passthrough Headsets*,
and "Only the Eye of the Bird" was a presentation device only. I will drop it from the title page
entirely rather than keep it as a subtitle — if I use it at all it will be as an epigraph to the
introduction, where it is clearly framing rather than nomenclature. If you think the academic
title still needs sharpening — it does lean on "visual noise cancellation", which is a borrowed
term rather than an established one — I am happy to work on it on Tuesday.

**2 · "Window".** You are right that it is doing two jobs, and I had not noticed how confusing
that is until you said it. There are genuinely two distinct things and I will name and define
them separately in Chapter 4, at first use:

- the **focus window** — the region the user places and anchors to the room, inside which the
  application draws nothing at all;
- the **detection aperture** — the smaller openings the detector creates over a recognised light,
  sign or person, which are transient and not user-controlled.

**3 · Filter names.** I would rather describe the operation than lean on a conventional name I
might be misusing, so in the dissertation I will lead with the mechanism and give the code name
in brackets on first mention only — for example "progressive peripheral blur with contrast
restoration (Blur)" and "warm–cool chromatic opposition (ChromaticCool)".

On the images themselves: I think most of what you saw is a slide artefact, but not all of it,
and I would rather check than assume. The grain mode is deliberately **not** white noise — it is
world-locked static grain at a fixed angular scale, so that the pattern stays put relative to
the room rather than crawling with the head, which is the property the source technique
(Cao et al., 2021) depends on. At the size it appeared on the slide that will alias and read as a
repeating texture. The blur mode is a genuine two-ring Gaussian, so any structure visible there
is compression. I will send you full-resolution stills of both this week so you can judge them
properly, and if the grain still reads wrong at full size then it is an implementation problem
and I will say so in the limitations rather than defend it.

**4 · The sickness point.** That came from the literature rather than from my sessions. Norouzi,
Bruder & Welch (2018) describe VR sickness in terms of nausea and vomiting, and report that
vignetting tied to head movement *increased* participants' sickness scores rather than reducing
them — but their measures were pre/post questionnaires and a discomfort rating each minute, so an
increase in scores is all that is reportable, and I will report it that way. I think that got
mixed in my head with what I mentioned to you earlier about one of my own participants feeling
unwell. To be clear on my own sessions: that participant took a break and then completed their
runs, no session was terminated, and nobody was sick. The only data excluded is the run that
failed the false-alarm criterion we discussed.

**5 · Ethics.** I will report the REAMS process properly, with submission and approval dates and
the reference number, in the methodology chapter. One question: my understanding is that I should
include the blank participant information sheet and consent form as an appendix, and retain the
signed originals securely rather than submit them, since they carry personal data. Could you
confirm that is what the School expects?

**6 · Demographics.** Being straight about this: I did not collect demographic data — the
post-session questionnaire has no age, gender or vision items, so I cannot report those. Rather
than estimate them retrospectively, I will report the inclusion criteria and the recruitment
route — all participants were over 18, recruited by direct approach from the postgraduate
computer-science cohort, unpaid, with normal or corrected-to-normal vision by self-report — so
that a reader can infer the sampling frame and its narrowness. I will list the absence of
demographic data as a limitation.

**7 · Carol's question on pedestrian velocity — no, it does not.** The detection aperture
responds to a detected object's **angular position and apparent size**, evaluated per frame, with
a minimum-lifetime gate and a smoothing fade so that boxes do not flicker in and out. There is no
velocity or trajectory term: a pedestrian stepping off a kerb and one standing still are treated
identically if they subtend the same angle. I will acknowledge this explicitly, since it is a
real limitation for the scenario the footage depicts — approach speed is exactly the cue a
driving-relevant system ought to use.

**8 · Overload.** There is a hard cap, and it differs by build, which I should be precise about:
the live passthrough path allows **8** simultaneous apertures, the video path used for the
driving scenario allows **16**. Slots are filled in the order detections arrive and any beyond
the cap are simply dropped — so there is a budget, but no prioritisation, which is the weaker
half of your point. Neither cap was reached in the reported sessions, but that is a property of
the footage rather than a design guarantee.

I will cover this in three places: the cap and its first-come behaviour in the system chapter,
the absence of prioritisation in limitations, and in future work the obvious remedy — ranking
candidates by urgency (eccentricity, apparent size, time-to-contact) and spending the budget on
the top few, so that a crowded scene degrades by dropping the least critical rather than the
latest. As you say there may be no clean solution; a filter that opens twenty holes has stopped
filtering, so at some density the honest answer is to disable it rather than dilute it.

**Two things I would like to ask.**

May I send you drafts over email for discussion? I would value your views on the writing as it
comes together, particularly before you go on leave.

And I would welcome your candid view on the presentation beyond the points above. I know I ran
over despite cutting it substantially, and I am not sure whether that read as thoroughness or as
too much detail — if any section felt rushed, bloated or unclear, that is the most useful thing
I could hear before writing the corresponding chapter.

Thanks again.

Best,
Shreyansh
