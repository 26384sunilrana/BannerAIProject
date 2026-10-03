# Bolt 042a - Personal and health information on ads, review, reports

Status: complete (first half of 042; audit export and the HIPAA review are 042b)

## Built
- **Screening of every ad text** (advertiser name, headline, text) each time an ad is saved:
  - Refused with the reason: phone numbers, e-mail addresses, card numbers (Luhn-checked), identity numbers (Aadhaar-style, US SSN, PAN) and dates of birth. Ordinary numbers (prices, years, dates, opening hours) pass.
  - Health or patient wording (patient, diagnosis, prescription, cancer, diabetes, HIV, mental health ...) does not refuse the ad: it is held for an **administrator's compliance review** before it can run.
- **New state "Waiting for compliance review":** an owner's or admin's ad goes there when it is sent in; an executive's goes to the owner first and then to the administrator. It holds its slot, is not on the screen, only an administrator can approve or send it back (reason needed), the owner cannot clear it. Editing re-screens the text and clears the review.
- Administrators get a bell message when an ad waits for review; owners hear of an administrator's ad only once it is approved.
- **Audit:** the ad history records each step (Submitted with the reason, Approved, Compliance approved/rejected, who, when, note).
- **Reports:** "Ad report" (admin): per person, ads booked in the month, went live, sent back, cancelled, waiting, held for review. The monthly statement now carries the shop's city and the admin sees totals by city.
- A stopped (overridden) ad can no longer be cancelled afterwards.
- Migration `AdCompliance`.

## Verified
- Domain 747 (35 new screening tests), Application 262, Integration 66 (5 new: refused details, owner review, executive then administrator, admin ad, user report; also on real SQL Server), Jest 755 (9 new), tsc and next build clean.
- Chrome against the real API, `compliance.js` 11/11: a phone number refused with the reason; the same ad without it saved; "flu shots for every patient" waits for review with no approve button for the owner and not on the screen; the administrator is told, approves, the history shows "Compliance approved by Ops Admin", the ad appears on the screen; the report shows 2 booked, 1 live, 1 held. `ads.js` re-run 21/21.

## Known gaps (the screening is a safety net, not a guarantee)
- Names of people, street addresses and free-text descriptions of someone's condition are not recognised; only numbers, e-mail, ids and health words are.
- Health words cause review for harmless ads too (a pharmacy). The administrator decides.
- Pictures are not looked at; text inside a picture is not read.
- The business phone number of an advertiser is refused as well; if you want business contact numbers allowed, that is a rule to change in `AdContentScreen`.
- Review only applies to ads; banners and the default board message are not screened yet.

## Not done (042b)
- Audit log retention and export, the HIPAA/PHI review of the whole application and the fixes it finds, re-encryption of old plain data.
