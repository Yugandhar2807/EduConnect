# 🤖 EduConnect Chatbot Implementation Guide

## Overview
The chatbot has been successfully integrated into EduConnect as a **floating widget** at the bottom-right corner of the page. It's a simple, round button that opens a chat popup when clicked.

## Features

### 1. **Floating Round Button**
- Located at bottom-right corner of all authenticated pages
- Gradient purple color (#667eea to #764ba2)
- Smooth hover animations
- Size: 60px diameter (scales on mobile to 50px)

### 2. **Chat Popup**
- Opens above the button when clicked
- Dimensions: 380px width × 500px height
- Mobile responsive: Full width on small screens
- Clean, modern interface with gradient header

### 3. **AI Responses**
The chatbot intelligently responds based on message content:
- **Greetings**: Friendly welcome messages
- **Course Inquiries**: Information about courses
- **Enrollment Help**: Guidance on enrolling in courses
- **Material Access**: Help accessing course materials
- **Quiz Support**: Information about quizzes
- **Grade Information**: How grades are calculated
- **Technical Support**: Common solutions for technical issues
- **Feedback**: Acknowledgment and appreciation

### 4. **Message Display**
- User messages appear on the right (blue)
- Bot messages appear on the left (white with border)
- Auto-scrolling to latest message
- Typing indicator with animated dots
- Smooth slide-in animations

### 5. **Database Integration**
- All conversations stored in `ChatMessages` table
- Tracks message category, sender, timestamp
- Supports sentiment analysis (future enhancement)

## Files Created/Modified

### New Files:
1. **Models/ChatMessage.cs** - Database model for storing chat messages
2. **Services/ChatbotService.cs** - Business logic for chat responses
3. **Controllers/ChatbotController.cs** - API endpoints for chat
4. **Views/Shared/_Chatbot.cshtml** - Chatbot UI widget

### Modified Files:
1. **Program.cs** - Added chatbot service registration
2. **Data/ApplicationDbContext.cs** - Added ChatMessages DbSet
3. **Views/Shared/_Layout.cshtml** - Included chatbot partial
4. **Migrations/** - New migration: AddChatbotSupport

## API Endpoints

### 1. Send Message
```
POST /api/chatbot/send-message
Content-Type: application/json

Request:
{
    "message": "How do I enroll in a course?"
}

Response:
{
    "success": true,
    "message": "How do I enroll in a course?",
    "response": "Bot response text...",
    "category": "EnrollmentHelp",
    "timestamp": "2025-10-21T10:30:00Z"
}
```

### 2. Get Chat History
```
GET /api/chatbot/history

Response:
{
    "success": true,
    "messages": [
        {
            "id": 1,
            "message": "Hi",
            "response": "Hello! Welcome...",
            "sender": "User",
            "timestamp": "2025-10-21T10:30:00Z"
        }
    ]
}
```

### 3. Get Quick Suggestions
```
GET /api/chatbot/quick-suggestions

Response:
{
    "success": true,
    "suggestions": [
        "How do I enroll in a course?",
        "Where can I find course materials?",
        ...
    ]
}
```

## How to Use

### For Users:
1. Click the round chat button at bottom-right
2. Chat popup opens with welcome message
3. Type your question in the input field
4. Press Enter or click send button
5. Receive instant AI response
6. Click X button or click outside to close

### For Developers:

**Add Custom Responses:**
Edit `Services/ChatbotService.cs` - `GenerateResponse()` method:
```csharp
if (lowerMessage.Contains("your-keyword"))
    return "Your custom response";
```

**Add Message Categories:**
Edit `Models/ChatMessage.cs` - `ChatCategory` enum:
```csharp
public enum ChatCategory
{
    // ... existing
    YourNewCategory  // Add here
}
```

## Message Categories

1. **General** - Default category
2. **CourseInquiry** - Course-related questions
3. **TechnicalSupport** - Technical issues
4. **EnrollmentHelp** - Enrollment questions
5. **GradeInquiry** - Grade/score questions
6. **MaterialAccess** - Course materials help
7. **QuizSupport** - Quiz-related questions
8. **Feedback** - User feedback/suggestions
9. **Other** - Miscellaneous

## Styling

The chatbot uses:
- **Font:** System fonts (Bootstrap default)
- **Colors:** 
  - Primary: #667eea (purple)
  - Secondary: #764ba2 (dark purple)
  - Background: #f8f9fa (light gray)
- **Animations:** Smooth transitions, slide-in effects, typing indicator

## Mobile Responsiveness

- **Small screens (<768px):**
  - Button: 50px (from 60px)
  - Popup: 100% width with 30px margin
  - Height: 60vh (up to 500px max)

## Security

- Chatbot only visible to authenticated users
- Uses ASP.NET Core [Authorize] attribute
- CSRF protection via anti-forgery tokens
- Input validation and sanitization

## Future Enhancements

1. **AI Integration**: Connect to real AI services (OpenAI, Azure OpenAI, etc.)
2. **NLP Improvements**: Better message classification
3. **Sentiment Analysis**: Track user satisfaction
4. **Human Handoff**: Transfer to support staff
5. **File Sharing**: Share documents/resources via chat
6. **Multi-language Support**: Translate responses
7. **Analytics Dashboard**: Chat statistics and insights
8. **Custom Branding**: Configurable colors and messages

## Testing

To test the chatbot:
1. Login to the application
2. Navigate to any authenticated page
3. Click the chat button at bottom-right
4. Try different questions:
   - "How do I enroll?"
   - "Where are the materials?"
   - "How is my grade calculated?"
   - "I'm having technical issues"

## Database Schema

**ChatMessages Table:**
```
Id (int) - Primary Key
UserId (string) - Foreign Key to AspNetUsers
Message (text) - User's message
Sender (int) - MessageSender enum (User=0, Bot=1)
Response (text) - Bot's response
Timestamp (datetime) - When message was sent
Category (int) - ChatCategory enum
Sentiment (double?) - Sentiment score 0-1
IsResolved (bool) - Whether issue was resolved
```

## Troubleshooting

**Chatbot button not appearing:**
- Ensure you're logged in
- Check browser console for JavaScript errors
- Verify _Chatbot.cshtml is included in _Layout.cshtml

**Chat not sending messages:**
- Check if authenticated
- Verify API endpoint is working: GET /api/chatbot/quick-suggestions
- Check browser console (F12) for errors

**Database issues:**
- Run: `dotnet ef database update`
- Check if ChatMessages table exists in database

## Configuration

To disable chatbot for certain users/roles, edit `_Layout.cshtml`:
```csharp
@if (User.Identity?.IsAuthenticated == true && !User.IsInRole("GuestRole"))
{
    @await Html.PartialAsync("_Chatbot")
}
```

---

**Status:** ✅ Ready for Production
**Last Updated:** October 21, 2025
**Version:** 1.0
