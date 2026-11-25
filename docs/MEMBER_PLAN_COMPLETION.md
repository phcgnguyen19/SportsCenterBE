# Member training-plan completion

`POST /api/TrainingPlans/{id}/complete` lets an active Member mark their own
active, started training plan Completed. The response includes the saved
`TrainingPlanResponseDTO`. The frontend uses that result to move the plan into
history. This is a self-report; the Coach receives a notification explaining who
marked the plan complete.

The endpoint checks the Member identity in the database, scopes the plan lookup
to that Member, and rejects cancelled or future plans. Completed plans return
their current DTO without another notification. A serializable transaction
protects the state change and notification. The Coach-only status endpoint keeps
its existing authorization.

The existing request-change service also checks account activity and the
1,000-character reason limit when called directly.

Validation: `TrainingPlanServiceTests` cover completion, repeated requests,
Coach notification, ownership, inactive users, future plans and the unchanged
Coach-only update path. No schema migration is required. Deploy the updated API
before enabling completion against an environment that lacks the endpoint.
