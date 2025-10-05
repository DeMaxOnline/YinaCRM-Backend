# SupportTicket Aggregate

## Overview

The `SupportTicket` aggregate represents a customer support request in the CRM system. It encapsulates all the business logic related to ticket management, including status transitions, assignment, priority management, and SLA tracking.

## Structure

### Core Entity
- **SupportTicket.cs**: The main aggregate root that manages ticket state and enforces business rules

### Value Objects

#### TicketNumber
- **Format**: `T-YYYY-NNNNNN` (e.g., T-2024-000001)
- **Purpose**: Unique, human-readable ticket identifier
- **Features**:
  - Pattern validation using regex
  - Generation method for creating new numbers with sequence
  - Year-based numbering scheme

#### TicketStatusCode
- **Allowed Values**: `new`, `in_progress`, `waiting`, `resolved`, `closed`
- **Purpose**: Tracks the current state of the ticket
- **Features**:
  - State transition validation
  - Predefined static instances for common use
  - Transition rules enforcement

#### TicketPriorityCode
- **Allowed Values**: `low`, `normal`, `high`, `urgent`
- **Purpose**: Indicates the urgency of the ticket
- **Features**:
  - Sort order calculation
  - High priority detection helper
  - Predefined static instances

### Domain Events

1. **SupportTicketCreated**: Raised when a new ticket is created
2. **SupportTicketAssigned**: Raised when ticket assignment changes
3. **SupportTicketStatusChanged**: Raised when ticket status transitions
4. **SupportTicketClosed**: Raised when a ticket is closed

## Key Properties

- **Id**: Unique identifier (SupportTicketId)
- **ClientId**: Reference to the client
- **HardwareId**: Optional reference to related hardware
- **CreatedByClientUserId**: User who created the ticket
- **AssignedToUserId**: Currently assigned support agent
- **Number**: Human-readable ticket number
- **Subject**: Brief description (required)
- **Description**: Detailed problem description
- **Status**: Current ticket status
- **Priority**: Urgency level
- **SlaDueAt**: SLA deadline timestamp
- **ClosedAt**: Closure timestamp
- **ToBill**: Billing flag
- **CreatedAt/UpdatedAt**: Audit timestamps

## Business Rules

### Ticket Creation
- Subject is required
- Ticket number must be unique and follow the format
- Default status is "new" if not specified
- Default priority is "normal" if not specified

### Status Transitions
Valid transitions:
- **new** → in_progress, waiting, resolved, closed
- **in_progress** → waiting, resolved, closed
- **waiting** → in_progress, resolved, closed
- **resolved** → closed, in_progress (reopen)
- **closed** → in_progress (reopen)

### Modification Rules
- Closed tickets cannot be modified (except for reopening)
- Subject cannot be empty when updating
- Assignment changes are tracked via events

## Usage Examples

### Creating a Ticket
```csharp
var ticketNumberResult = TicketNumber.Generate(sequenceNumber, DateTime.UtcNow);
var titleResult = Title.Create("Cannot access email");
var bodyResult = Body.Create("User reports email client not connecting...");

var ticketResult = SupportTicket.Create(
    clientId: clientId,
    createdByClientUserId: userId,
    number: ticketNumberResult.Value,
    subject: titleResult.Value,
    description: bodyResult.Value,
    priority: TicketPriorityCode.High
);
```

### Changing Status
```csharp
var result = ticket.ChangeStatus(TicketStatusCode.InProgress);
if (result.IsFailure)
{
    // Handle invalid transition
}
```

### Assigning Ticket
```csharp
var result = ticket.AssignTo(supportAgentId);
// This will raise SupportTicketAssigned event
```

## Design Decisions

1. **No Persistence Concerns**: The aggregate is pure domain logic with no database dependencies
2. **Result Pattern**: All operations return `Result<T>` for explicit error handling
3. **Event Sourcing Ready**: Events are collected and can be dequeued for processing
4. **Immutable Value Objects**: All VOs are immutable structs with validation
5. **UTC Timestamps**: All date/time values are stored in UTC
6. **State Transition Validation**: Business rules enforce valid status transitions

## Testing Considerations

- Test all status transition combinations
- Verify event generation for each operation
- Validate ticket number generation and format
- Test closed ticket modification restrictions
- Verify priority sorting logic
- Test SLA date handling
