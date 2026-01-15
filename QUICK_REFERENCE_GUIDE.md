# Quick Reference: How to Use New Features

## FOR FACULTY - Mark Attendance

### 1. Navigate to Attendance Management
   - URL: `/Faculty/ManageAttendance`
   - Click on your course from the dropdown

### 2. View Attendance Records
   - See all past attendance entries for your course
   - Shows: Student name, Date, Status, Remarks
   - Edit or Delete any record

### 3. Mark Today's Attendance
   - Click "Mark Attendance Today" button
   - Select status for each student (Present/Absent/Leave)
   - Add optional remarks
   - Click "Save Attendance"

### 4. Edit Previous Attendance
   - Click edit icon on any record
   - Change status or remarks
   - Save changes

---

## FOR ADMIN - Manage Semester Results

### 1. Navigate to Semester Results
   - URL: `/Admin/ManageSemesterResults`
   - Use search to find specific student or semester

### 2. Add New Semester Result
   - Click "Add New Semester Result"
   - Select Student
   - Choose Semester (Fall 2025, Spring 2026, etc.)
   - Enter Course Name
   - Enter Marks (0-100)
   - Select Grade (A, B, C, D, F)
   - Enter GPA (4.0 scale)
   - Add optional remarks
   - Click "Save Result"

### 3. View All Results
   - URL: `/Admin/SemesterResultsList`
   - Filter by student or semester
   - View GPA color-coded:
     - Green badge: GPA 3.8+
     - Blue badge: GPA 3.0-3.7
     - Yellow badge: GPA <3.0

### 4. Edit/Delete Results
   - Click edit icon to modify
   - Click delete icon to remove (confirmation required)

---

## FOR STUDENTS - View Dashboard

### 1. Student Dashboard
   - URL: `/Student/Dashboard`
   - See at a glance:
     - **Enrolled Courses**: Total courses you're taking
     - **Completed Courses**: Courses finished
     - **Attendance**: Your average attendance percentage
     - **Average GPA**: Your calculated GPA from all semesters

### 2. Latest Semester Results Section
   - Shows your 5 most recent semester grades
   - View: Semester, Course, Marks, Grade, GPA
   - Color-coded GPA badges

### 3. Recent Attendance Section
   - Shows your 5 most recent attendance records
   - View: Course, Date, Status, Remarks
   - Status colors:
     - Green: Present
     - Red: Absent
     - Yellow: Leave

### 4. Course Progress Summary
   - All your enrolled courses in cards
   - Progress bar showing completion %
   - Topics completed out of total
   - Quiz attempts and average score
   - Status: Not Started / In Progress / Completed

---

## Data Entry Tips

### Faculty Tips for Attendance
- ✅ Mark attendance each day or once a week
- ✅ Add remarks for absences (Sick, Family Emergency, etc.)
- ✅ Edit if you make a mistake immediately
- ✅ Mark "Leave" for approved absences (not Absent)

### Admin Tips for Semester Results
- ✅ Enter marks out of 100
- ✅ Calculate GPA according to your institution's scale
- ✅ Use remarks to note achievements or concerns
- ✅ Add results for all courses each semester
- ✅ Update if grades change after review

### Student Tips for Dashboard
- 📊 Check your attendance percentage regularly
- 📈 Monitor your GPA and course progress
- 📋 Review recent grades and marks
- 🎯 Plan to improve attendance and performance

---

## Calculation Methods

### Attendance Percentage (Auto-Calculated)
```
Attendance % = (Present Days / Total Days) × 100
```
Example: 4 Present out of 5 days = 80%

### Average GPA (Auto-Calculated)
```
Average GPA = Sum of All Semester GPAs / Number of Semesters
```
Example: (4.0 + 3.5 + 3.8) / 3 = 3.77

### Course Progress (Auto-Tracked by System)
```
Completion % = (Topics Completed / Total Topics) × 100
Average Score = Sum of Quiz Scores / Number of Quizzes Taken
```

---

## Status Indicators

### Attendance Status
- **Present** ✅ Green badge
- **Absent** ❌ Red badge
- **Leave** ⏸️ Yellow badge

### Course Progress Status
- **Completed** ✅ Green badge
- **In Progress** 🔵 Blue badge
- **Not Started** ⚪ Gray badge

### GPA Performance
- **A (3.8-4.0)** 🟢 Green badge - Excellent
- **B (3.0-3.7)** 🔵 Blue badge - Good
- **C+ (2.5-2.9)** 🟡 Yellow badge - Satisfactory
- **Below (< 2.5)** 🔴 Check remarks

---

## Common Issues & Solutions

### Issue: Can't access Attendance Management
**Solution:** You must be logged in as Faculty. Contact Admin if role is incorrect.

### Issue: Student not appearing in dropdown
**Solution:** Student must be enrolled in your course first.

### Issue: Can't edit old attendance records
**Solution:** You have permission to edit any record. Check if courseId is correct.

### Issue: Can't create semester result
**Solution:** Make sure to select a student, fill all required fields, and enter valid GPA (0-4.0).

### Issue: Attendance percentage not updating on dashboard
**Solution:** Dashboard calculates from existing attendance records. New marks appear after saving.

---

## Database Fields Reference

### Attendance Record
```
StudentId       - Student's user ID
CourseId        - Course ID
AttendanceDate  - Date (YYYY-MM-DD)
Status          - Present / Absent / Leave
Remarks         - Optional notes
CreatedAt       - Automatically set
```

### Semester Result
```
StudentId       - Student's user ID
Semester        - e.g., "Fall 2025"
CourseName      - Subject name
MarksObtained   - 0-100
Grade           - A/B/C/D/F
GPA             - 0-4.0
Remarks         - Optional notes
CreatedAt       - Automatically set
UpdatedAt       - On edit
```

### Course Progress
```
StudentId           - Student's user ID
CourseId            - Course ID
EnrollmentDate      - When enrolled
TopicsCompleted     - Number completed
TotalTopics         - Total in course
CompletionPercentage - 0-100%
QuizzesTaken        - Quiz attempts
AverageScore        - Average score
ProgressStatus      - Not Started / In Progress / Completed
LastActivityDate    - Last interaction
```

---

## For Power BI Integration

When connecting Power BI to the database, import these three tables:
1. **Attendances** - For attendance dashboards
2. **SemesterResults** - For academic performance dashboards
3. **StudentCourseProgresses** - For course engagement dashboards

All tables link via **StudentId** and **CourseId** to ApplicationUser and Course tables.

