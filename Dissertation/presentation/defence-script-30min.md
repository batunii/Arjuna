# Only the Eye of the Bird — 30-minute defence script

Deck: `Only-the-Eye-of-the-Bird-TIGHT.pptx`, 40 slides.

**Calibration.** 3,845 spoken words — 30 minutes of speech at 130 words/min, plus clip playback, totalling 30:00 across 40 slides. Budgets come from the real word counts, not guesses, so they are tight: ad-lib a sentence and you must drop one elsewhere. S16 is marked skip-by-default to make the arithmetic work. Times are **cumulative** — check the clock at the three ⏱ marks.

**Part boundaries:** Part 01 → 5:58 · Part 02 → 7:51 · Part 03 → 19:08 · Part 04 → 22:53 · Part 05 → 29:36 · close 30:00. Part 03 takes a third of the talk on purpose — the system, the novelty and the equivalence argument.

**Depth is deliberate.** Full detail on the inspiration, the research gap, the
implementation, the novelty (window-as-absence, SignPop, adaptiveness), the study, the
results and their validity. Everything else is a signpost — say the line and move.

---

## Part 01 · The idea → 5:58

### S1 · Title — 0:25 → 0:24

> Good afternoon. This is *Only the Eye of the Bird* — a system that tries to help you
> concentrate not by adding anything to what you see, but by taking things away. It runs on
> a Meta Quest 3 you can buy in a shop, and eleven people tested it over four days.

### S2 · Part 01 divider — 0:10 → 0:35

> It starts with a story, and then about a hundred and ten papers that pruned the design
> before I wrote a line of it.

### S3 · The origin — 0:55 → 1:31

> There is a scene in the Mahābhārata. Droṇa sets his students one test: hit the eye of a
> wooden sparrow in a distant tree. Before each shot he asks what they can see. One says the
> tree. One says the branches, the leaves, his brothers. Arjuna says: "only the eye of the
> bird." He is the only one told to shoot.
>
> What stayed with me was not the archery but the *absorption* — a person so far inside a task
> that everything else has stopped arriving. I have had sparks of it, fifteen minutes at a
> time, and you cannot summon them back on demand.
>
> So: if that state is everything else falling away, can a headset make some of it fall away
> *for* you?

### S4 · Demo — 1:15 → 2:45  🎬

**Start the clip. Silence for the first five seconds. Then talk over it.**

> On device, no post-processing. Three things to watch.
>
> **The window in the middle.** The application draws nothing inside it. Not "transparent" —
> *nothing*. I will come back to why that matters more than anything else in the build.
>
> **The edges.** One shader fades the world outside. Nine of these filters were built; two
> were formally tested.
>
> **The traffic light.** A detector opens a hole in the filter over signals and signage, so
> the things you must not miss come through.
>
> Everything after this slide answers the question *why does it look like that.*

### S5 · We already do this — 1:00 → 3:45

> The idea is not really mine. Two of our three main senses already answer distraction, and
> both answers are subtractive.
>
> **Space** — the library carrel. Nothing is added; the room is subtracted until only the page
> is left. **Sound** — noise-cancelling headphones: not louder lectures, everything else
> quieter. **Sight** — nothing. Every visual aid on the market *adds*.
>
> The literature calls the missing thing *visual noise cancellation*, and it is a literal
> analogy: camera → processing → display has the shape of microphone → processing → speaker.
> It is possible now because every pixel on a passthrough headset is already synthetic.
>
> But an intuition is not a finding. Three things had to be true: the clutter must cost
> something, removing it must give that back, and it must run on hardware people own.

### S6 · Evidence for — 0:55 → 4:42

> The first is settled, and it matters *how*.
>
> Ai and colleagues, last year, sixty-six people. With peripheral clutter present: two point
> four times more wrong presses, eight times more misses — and **speed did not change at all.**
>
> That one fact set my outcome measure. The cost of distraction shows up in *mistakes*, not in
> slowness, so accuracy is the primary measure and it was fixed before collection.
>
> Second, somebody had already dimmed distractions: McLaughlin and colleagues, assembly under
> distraction. Dimming *everything* irrelevant beat dimming only the offender. The blunt
> instrument won — hold that until we reach the smart filter.
>
> The mechanism is ordinary cognitive load. And the reason to *dim* is Itti and Koch:
> attention chases contrast, so flatten it and the periphery stops shouting.

### S7 · What the evidence ruled out — 1:15 → 5:58

> This is the slide I would point at if you asked what the literature actually *did* here. It did
> not motivate the design — it pruned it. Three of the four most obvious ways to build a visual
> filter are contraindicated by published evidence. Three examples:
>
> **Whole-periphery blur** — the obvious first idea, and the most *noticed* of five guidance
> techniques: fifty-nine of a hundred and two people spotted it, against twelve for a red dot.
> So blur got built and never carried into testing.
>
> **Vignetting tied to head movement** — increased sickness in eleven of fifteen people, who also
> preferred the condition without it. So I inverted it: my effect *yields* while you move.
>
> **Total removal** — anxiety rose in every narrowed field across a hundred and sixteen people. So
> blackout is the extreme of a range, not the default.
>
> And the last row set the target. Sutton and colleagues: no manipulation of this kind goes
> unnoticed. So I stopped chasing invisibility and designed for *noticeable but tolerable*.

**⏱ Checkpoint: should read 5:58. If more than 45 seconds past it, drop S36 as well.**

---

## Part 02 · The walls → 7:51

### S8 · Part 02 divider — 0:10 → 6:08

> On the hardware people actually own, the obvious way to build this is not difficult. It is
> forbidden, three times over.

### S9 · Walls 1 and 2 — 1:00 → 7:08

> The room reaches you as a camera feed, and the operating system assembles what you see. You
> get three levels of access.
>
> **Level one** — recolour the whole view. Free, and useless here: it is uniform or nothing, so
> I cannot dim *this* corner of the room.
>
> **Level two** — draw over it. Your own shapes, in front. You may darken, black out or leave a
> hole; you may not read the real pixels and you may not modify them.
>
> **Level three** — take the camera feed. Anything you like, pixel by pixel, at a price.
>
> And the wall is deliberate — privacy and safety. Apple is stricter still. So nothing *local*
> is possible at level one, which leaves level two: draw over the world, and leave a hole where
> you want the truth.

### S10 · Wall 3 — 0:45 → 7:51

> Here is the price of level three. The camera you are given sees less than the view it would
> replace: one camera instead of two, twelve-eighty by nine-sixty, and about eighty-seven
> degrees against the hundred and ten you already had — twenty-three degrees narrower each
> side, and you lose the far edge.
>
> So the straightforward version of this project backfires. Paint the processed feed over
> everything and you hand the user a worse world in exchange for a filter.
>
> Which makes the real engineering question: **how do you process the edges without degrading
> the middle?**

---

## Part 03 · The system → 19:08

### S11 · Part 03 divider — 0:05 → 7:57

> One idea does most of the work, and it is close to a trick.

### S12 · The inverted sphere — 1:10 → 9:10  ⭐ NEW

> First, what the filter is actually painted on — because it is not a screen.
>
> It is a sphere, centred on your head, and you are standing inside it. One line of render state
> throws away the outward-facing triangles, so what you see is its **inner** surface. It is
> re-centred on your head every frame, so it cannot be walked out of, and the radius is
> arbitrary — the shader never uses distance.
>
> What it uses is **direction**. Every fragment becomes an azimuth and an elevation, and that is
> the whole reason this works: a direction is the same for both eyes, and it belongs to the room
> rather than to your screen. So the window can stay on the desk while the shell follows you.
>
> It is also how the platform wall gets obeyed — drawn in front, ordinary alpha blend, the room
> never read and never modified. And the window is simply a wedge where alpha is zero.

### S13 · Window-as-absence — 1:05 → 10:16  ⏱

> The best way to show somebody reality is to draw nothing at all.
>
> Inside the window, the application draws nothing. So what reaches the eye is the headset's
> own passthrough: full quality, the full hundred and ten degrees, both eyes, no added latency
> — and it costs nothing, because I did not do anything.
>
> And that is what answers the camera wall. Look at the curve: zero out to eight degrees, half
> strength at twenty, full from thirty-two. Every bit of processing is spent at the edges —
> exactly where the narrow mono camera is good enough, because it is also where your eyesight
> is worst. The wall stops mattering.
>
> As far as I can establish, this is new. I searched the major venues from 2022, the platform
> documentation, and all hundred and thirty-six public forks of Meta's own camera-access
> sample. Nobody uses absence as the high-fidelity region.

### S14 · The same thing, on the device — 0:25 → 10:41  🎬

**Play the IRLFILTERS clip. Say the line, then let it run.**

> And this is that window in a real room, on the headset, with nothing drawn inside it.

### S15 · Where the window lives — 0:50 → 11:30

> The window has to belong to the room, not to your head.
>
> What you get for free is screen coordinates — the window follows your gaze like a torch beam.
> Useless for a desk task, and actively harmful: head-coupled restriction is what made people
> sick in the study I just cited.
>
> Better is locking to a direction, so it stays over the desk when you turn. But not when you
> walk.
>
> Best, and what the study used: anchored to the room's real geometry. On release I cast four
> corner rays and remember where they land as physical points, so you can walk toward it,
> around it, away from it.

### S16 · One camera, two eyes — 0:15 → 11:45

**SKIP BY DEFAULT.** The sphere slide already made the direction argument, and this is the
first thing on the cut list. Click through with the one-liner below; keep the full version in
your pocket for Q&A, because it is the best "I found this on a device" story you have.

> Sampled by screen position this was correct in each eye and ghosted across both — the fix is
> the direction idea from the sphere. Which you can only discover with a person inside it.

**Full version, if asked:** the original code sampled the camera by screen position. Per eye that
is correct — but your two eyes see the same point in the room at different places on their
screens, so each got a different camera pixel for the same object, and everything ghosted.
Sampling by world direction fuses, like paint on the walls. Per eye the code was correct; it
failed only as a binocular system, on a device, with a person inside it.

### S17 · Why adaptive — 0:35 → 12:19

> Now the part I think is the actual contribution.
>
> Good noise cancelling does not mute everything. It passes the announcement through and
> suppresses the rest, and nobody calls that a compromise — it is the point.
>
> So the visual version cannot be a constant. Dim the street, but let the traffic light
> through. A constant, always on, at full strength, is not noise cancellation — it is a
> blindfold. **Deciding what passes is the design.**

### S18 · Three kinds of adaptation — 0:55 → 13:16

> Three things the filter adapts to.
>
> **What is in the scene.** A detector finds traffic lights and signs; the filter lifts each one
> off, pushes it forward and darkens a ring around it. For the study the live detector was
> replaced by a pre-computed record of every sign and light in the footage, so every participant
> saw identical, machine-timed events.
>
> **How bright it is.** Glare rolls off instead of clipping, with a ring test that exempts real
> lamps so I do not crush the thing I am preserving.
>
> **What you are doing.** The filter yields while you move — out in two tenths of a second when
> you turn, back over seven tenths once you settle. The inversion of the vignetting finding from
> Part 01.

### S19–S20 · The filter family — 0:40 → 13:55  🎬

**Let the clips run while you talk. Do not narrate individual cells.**

> Nine ways to fade the edges of the world, in one shader.
>
> That is possible because of the geometry two slides ago: a filter here is *one rule* for
> colour and opacity as a function of angle outside the window. Once the window and the
> falloff exist, a new filter is a branch, not a project.

### S21 · Why nine, and what the sources did — 2:15 → 16:11  ⭐

> Which raises the obvious question — why build nine? Why not read the papers, pick the
> winner, and test that?
>
> Because none of these could be judged on paper. Look at the middle two columns.
>
> **Subtle Gaze Direction**, which three of my filters descend from, runs on an eye tracker, chin
> on a rest, seventy-five centimetres from a monitor — and the modulation is killed the instant a
> saccade heads toward it. That gaze-contingency *is* the technique. There is no eye tracker on
> this headset, so my Chromatic Cool keeps the colour trick and loses the mechanism.
>
> **Veas** tuned saliency modulation to the threshold of invisibility, on a monitor, lights off. I
> ran the same rule on a live camera in a room I do not control — and flattening toward the local
> mean lifts any pixel darker than its neighbours, so my periphery got *brighter*. My artefact,
> not a fault in their finding.
>
> **Sutton**, the closest predecessor, uses see-through glasses, which can add light but never
> subtract it — and they rule out video passthrough for costing fidelity. That objection is the
> gap I build in: on video I *can* subtract, and drawing nothing keeps the fidelity they said
> video would cost.
>
> **Cao's** grains I reproduced exactly, but they measured search time in a synthetic scene. My
> task needs the legibility of the signal itself, and grains stipple over precisely that.
>
> So: every technique here needed an eye tracker, a calibrated monitor, or a synthetic scene.
> Not one had run on a live camera feed on consumer hardware. Porting was the only way to find
> out which survive — and **three of the nine were decided by the port, not the paper**:
> Flatten, Outlined Dark and Spot Lift. That is why the family is wide and the tested set is
> narrow.

### S22 · Pruning, tier 1 — published evidence — 0:40 → 16:50

> So, the pruning — in named tiers, because they are not equally evidenced and I would rather
> say so than have you ask.
>
> Two were rejected because the literature had already ruled them out. **Blur** — detected
> through the contrast collapse it causes, the most noticed of five techniques, and also my
> heaviest shader path. **Chromatic Cool** — the sharpest one: it is the losing arm of its own
> source study. Bailey tested luminance against warm-cool chroma and luminance won. The mode
> implements the arm that lost.

### S23 · Pruning, tier 2 — observed in use — 1:00 → 17:50

> Three more were rejected because I watched people use them. **Conspicuity Squeeze** brightened
> the periphery. **Outlined Dark** was more interesting than the task. **Granulated** lost an
> informal head-to-head against SignPop. And two never got a fair hearing: Spot Lift could not be
> built at all, and Soft Dark did not fit the session budget.
>
> Now the honest wording. Two of those three are **settings-dependent, not technique-dependent**,
> and I did not sweep the parameters — so the claim is "rejected at the settings piloted". None of
> these is a dead idea. Each lost a place in *one* study; all are still in the shader, and I would
> expect several to win on a different task. Grain is probably right where event presence matters
> and fine detail does not — Cao's own result.

### S24 · What survived — 1:20 → 19:08

> Two went into testing: the strongest and the smartest.
>
> **Hard Dark** — total blackout outside the window. I chose it for the real-hardware arm
> because it is the maximum of the range, so it maximises the chance of detecting the mechanism
> if it is there, and it is pure luminance change, so nothing but brightness differs.
>
> **SignPop** — built for this work, and the one I would defend as novel. A composite, not a
> variant: ColorPop's salience grading gated by the detector, plus a highlight with a darkened
> surround and a roughly one-hertz pulse.
>
> It exists because of a claim-validity problem. ColorPop pops anything sufficiently red — brake
> lights, neon, a red jacket. But my claim is not "red things become easier to see", it is that
> *road-relevant signals* do, and colour class cannot make that distinction. So the keep is gated
> by detection — and when the detector misses, the signal degrades to dimmed but never hidden. A
> filter that can hide a traffic light it failed to recognise is not shippable.

---

## Part 04 · The study → 22:53

### S25 · Part 04 divider — 0:05 → 19:13

> One test on real hardware, one in simulation, the same eleven people.

### S26 · What is actually being claimed — 1:25 → 20:37

> This is the reframing, and the thing I would most like understood.
>
> This is **not** a filter that makes you more attentive. Dimming adds no capacity and trains no
> skill; in a quiet room it offers you nothing.
>
> It is a filter that stops *something else* spending your attention. Suppress what steals
> headroom and you get that headroom back — which means the benefit **needs distraction to
> exist**. A narrower claim than people expect, and the one the study was registered with.
>
> So: two questions. On real hardware, the benefit — can you do less badly under distraction? In
> simulation, the price — does the smart version work without costing you awareness?
>
> Why one of each: live passthrough can never repeat the same peripheral event twice, so I cannot
> measure awareness against ground truth. The video sphere can, machine-timed.
>
> **And driving is only the footage.** The case is any task you do while the space around you
> still has to be tracked — cycling, walking a factory floor, a busy kitchen. What generalises is
> the *demand*, not the steering wheel. I make no claims about road safety.

### S27 · The hypotheses — 0:50 → 21:29

> Both fixed before collection, each stated with what would refute it. **H1** — accuracy under
> distraction is higher with the filter. **H2** — awareness of peripheral events is not
> meaningfully worse; specifically, not more than ten points worse.
>
> That ten-point margin is the methodological point. "We found no difference" is a weak claim
> — you can get it by running a bad experiment. Naming the smallest loss I would actually care
> about, *before* collecting, turns it into a claim that could have failed. It is a
> pre-registered judgement, not a number from a paper — none of this literature supplies one
> — and Lakens is explicit that it has to come from the researcher.

### S28 · How each test worked — 0:50 → 22:18  🎬

**Play the clip; talk over it.**

> On real hardware: shapes one at a time, press when one matches the one immediately before.
> It holds and refreshes memory on every trial, which is what makes it demanding.
>
> In simulation: ring markers at known angles on a moving scene, press when you notice one.
> Onset and catch are machine-timed, following the ISO detection-response-task standard.
>
> And one check I did before the analysis, because it is the first thing I would attack: the
> markers are drawn *through* the filter, not on top of it. So in the filtered condition they
> were harder to see, not easier. The test is biased against my own hypothesis.

### S29 · What was collected — 0:35 → 22:53  ⏱

> Twelve people over four days, eleven usable pairs per task.
>
> One thing went wrong, and it is worth stating. On day one the memory task was too easy —
> nearly everyone scored close to perfect, so there was no room left to show an improvement.
> A ceiling. I made it harder for the remaining sessions, and both phases are marked
> separately on every chart. Two independent samples agreeing is the nearest thing this study
> has to a replication.

---

## Part 05 · What we found → 29:36

### S30 · Part 05 divider — 0:05 → 22:59

> Focus up, awareness intact, and one result that needed a lot of checking.

### S31 · Result 1 — the memory task — 0:50 → 23:50

> H1 supported. Accuracy up three point six five points — ninety-two point six to ninety-six
> point three. Every line on that chart is one person against themselves; nine of eleven
> improved. The interval excludes zero, p equals nought three six, Wilcoxon nought two four.
>
> And look at the phase split, because it is the ceiling argument paying off. The pilot gained
> two point two points from ninety-seven point two — where only two point eight were even
> *available*. The harder version gained four point five. Making the task harder roughly
> doubled the measured benefit, which is what you would predict if the effect is real and day
> one was simply out of room.

### S32 · Result 2 — the price — 1:05 → 24:55

> This is the result that matters most, even though it is the less exciting number.
>
> Awareness of the surroundings was not lost. Across the whole field of view it went *up* by
> three point nine points. The interval runs from minus four point nine to plus twelve point
> six — so against my pre-registered ten-point line, the worst case the data supports is about
> five points lost.
>
> Why it matters: a focus aid that works by blinding you is not a focus aid, it is a hazard.
> Every study here documents the same focus-versus-awareness trade-off. This is the first
> measurement of it on real consumer hardware — the two nearest published studies both had to
> simulate diminished reality inside VR — and it comes out better than the literature predicts.
>
> But a pooled number mixes regions the filter barely touches with regions it owns completely.

### S33 · Result 3 — the 20–30° ring — 0:55 → 25:51

> Split by angle, and the shape of this is the finding.
>
> Under ten degrees, where there is no filter to lift: nothing. Ten to twenty, at twenty
> percent strength: plus two point six. **Twenty to thirty, where the filter is at
> seventy-five percent: plus eighteen point six points** — forty percent up to fifty-eight.
> Beyond thirty: nothing again.
>
> It survives the phase split, too — pilot plus twenty-five, extra phase plus fifteen. The
> only band that agrees across both.
>
> One method point that changed the answer: markers travel about seventeen degrees while
> visible, and forty-five of eighty-two cross a band boundary. So every press is scored against
> where the marker actually was at that instant. Had I averaged, this would have smeared away.

### S34 · Why that ring — 1:15 → 27:05  ⭐ NEW

> The same result as geometry rather than bars, because the shape is the argument.
>
> Rings are degrees out from the centre of the window; the shade is how hard the filter works at
> that angle. Two things have to be true at once for any of it to buy you something.
>
> **The filter has to be acting** — inside ten degrees it is at zero, so there is nothing to
> lift, and that band moved minus one. **And your eye has to still be able to use the
> improvement** — beyond thirty degrees it cannot, and that band also moved minus one.
>
> Those two conditions overlap in exactly one place, twenty to thirty degrees, and that is where
> the entire effect sits.
>
> And this shape was not chosen after the fact: the band edges come from published gaze
> landmarks fixed before the analysis, and the strengths come from the falloff shipped in the
> build. A filter that helped everywhere would not fit its own geometry.

### S35 · Reading the results — 0:50 → 27:57

> Briefly, what those numbers are worth, since I have been throwing them at you.
>
> The **p-value** — how often chance alone would produce a gap this big if the filter did
> nothing. About one run in twenty-eight. The **confidence interval** — the range the data
> supports. It excludes zero, so the effect is real; it is six point seven wide, so its *size*
> is not pinned down. The **Wilcoxon** is the assumption-free check, and it agrees.
>
> And the honest one — **power nought five nine.** I had a fifty-nine percent chance of detecting
> an effect this size; seventeen people would have reached the conventional eighty. What eleven
> people cannot buy is a narrow interval.

### S36 · Result 4 — the questionnaire — 0:15 → 28:14

> What people *said*, in one picture. Clear focus benefit — ninety-four percent of answers
> favoured the filter. Middling comfort. And a noticeably softer awareness score, which is an
> interesting disagreement with the measurement, and belongs in the discussion.

### S37 · What actually caused it — 0:40 → 28:54

> One analysis, because "it got better" is not a mechanism.
>
> It was not the dimming. Seventy of the eighty markers — eighty-eight percent — sit inside an
> active detection window, and in the no-filter arm the detector is suppressed. The marker
> itself stays dimmed; the *object underneath it* pops. People were spotting the traffic light,
> not a brighter marker. The pass-through is the effect.
>
> The price of that clarity: the manipulation is a bundle. Filter-on switches everything at
> once, and no number here splits the eighteen point six.

### S38 · Where this leaves things — 0:40 → 29:36

> Built: a subtractive interface on a headset you can buy, on a platform that forbids touching
> reality at all. Found: focus up three point six five; awareness not lost; and the mechanism
> showing up in the ring the filter aims at.
>
> Not claimed: the bundle. Eleven people, not twenty. Half the participants felt cut off and
> nothing in my instruments measures that. Driving is the footage, not the claim.
>
> Next: more participants to tighten every interval, more targets in the thin bands, and a way
> to measure the feeling of being cut off.

### S39 · The argument, end to end — 0:20 → 29:55

> Which is the whole argument. Two senses answer distraction by subtraction and sight has
> none. The platform lets you draw in front of reality and nothing else. So draw nothing where
> it matters — and the edge of vision got better, not worse.

### S40 · Thank you — 0:05 → 30:00

> Thank you. Any of the clips can be replayed.

---

## If you are running long

Cut in this order — each is self-contained and nothing downstream depends on it:

1. **S36** questionnaire (−20s). One line: "the subjective data agrees on focus, softer on
   awareness." (**S16** is already skipped by default — the 30:00 arithmetic assumes it.)
3. **S15** down to two sentences (−35s): "head-locking makes people sick, so it is anchored to
   the room's real geometry."
4. **S35** keep only power (−35s): "both results clear it on two tests; power was fifty-nine
   percent, so the intervals are wide."
5. **S5** down to the carrel and the headphones (−30s).
6. **S22** drop the Blur half (−20s), the slide shows it.

Do **not** cut S7, S12, S13, S21, S26, S32, S33 or S34 — the research gap, the novelty, the equivalence
answer, the claim, and the two results that carry it.

---

## Questions you have invited

**"Why nine filters? Isn't that unfocused?"**
S21 answers it. Every source technique needed something the Quest 3 does not have — an eye
tracker, a calibrated monitor, a synthetic scene. Breadth in the build was the only way to
find out which survive the port; depth in the test was forced by a fifty-three-minute session.
Three of nine — Flatten, Outlined Dark, Spot Lift — were decided by the port rather than the paper.

**"You rejected six techniques from published literature. Isn't that arrogant?"**
No, and the tiers matter. Two on published evidence — Chromatic Cool is the losing arm of its
own source study. Three on observation, two of which are settings-dependent and were not
swept, so the claim is "rejected at the settings piloted". One on platform constraint. Every
one is still in the shader, and I would expect several to win on a different task.

**"Did you implement Cao / Veas / Cheng faithfully?"**
Cao — yes, parameters exactly; the task does not transfer. Veas — faithfully to the rule, and
the rule brightened my periphery, which is a porting artefact I can point to in the shader.
Cheng — no, and I can say why: they had 3-D scanned geometry, I have screen-space edges, so my
outlines carry a style theirs did not.

**"So implementation difficulty decided your design?"**
Partly, and I would rather own it than hide it. Spot Lift was impossible — the OS passthrough
layer cannot be brightened. Blur was the heaviest path in the family. Conspicuity Squeeze and
Outlined Dark both failed on artefacts of *my* port rather than on the original findings. That
is a finding about porting a display-based literature onto consumer passthrough, and it is
worth reporting as one.

**"Is this really about driving?"**
No. The footage is a drive because I needed repeatable, machine-timed peripheral events. The
demand it stands for is task focus while the surroundings still matter — cycling, walking a
floor, a kitchen, a workshop.

**"Eleven people is not many."**
Correct, and I would rather say it than have it extracted from me. Power fifty-nine percent;
seventeen would reach eighty. What I have is two independent phases agreeing, two tests with
different assumptions agreeing, and an effect appearing in the band the shader's geometry
predicts and nowhere else.

**"Which filter is best?"**
Unanswered, deliberately. Hard Dark is the strongest manipulation; SignPop is the one whose
measurement matches the claim. Comparing them to each other needs a session budget I did not
have.
