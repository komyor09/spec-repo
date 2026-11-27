import http from 'node:http';

const port = Number(process.env.PORT || 3000);

const payload = {
  service: 'Академия Вектор — ASP.NET Core 8 Web API',
  subject: 'ООП на C# (.NET 8)',
  note: 'Основной проект находится в AcademyVector.sln и папке backend/ (запуск: dotnet run --project backend/AcademyVector.Api).',
  storage: 'data/courses-db.json',
  endpoints: [
    'GET /api/health',
    'GET /api/platform/oop-summary',
    'GET /api/bootstrap?role=guest|student|teacher',
    'POST /api/inquiries',
    'POST /api/courses/{courseId}/enroll',
    'POST /api/courses/{courseId}/lessons/{lessonId}/complete',
    'POST /api/courses/{courseId}/lessons/{lessonId}/note',
    'POST /api/courses/{courseId}/quizzes/{quizId}/submit',
    'POST /api/courses/{courseId}/assignments/{assignmentId}/submit',
    'POST /api/courses',
    'POST /api/courses/{courseId}/modules',
    'POST /api/courses/{courseId}/modules/{moduleId}/lessons',
    'PUT /api/courses/{courseId}/lessons/{lessonId}',
    'POST /api/courses/{courseId}/modules/{moduleId}/quizzes',
    'PATCH /api/homeworks/{homeworkId}/review',
    'POST /api/groups',
    'PATCH /api/students/{studentId}/group',
    'POST /api/webinars',
    'DELETE /api/webinars/{webinarId}',
    'PATCH /api/payments/{paymentId}/confirm',
    'POST /api/reset',
  ],
};

const server = http.createServer((req, res) => {
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  if (req.url === '/api/health') {
    res.writeHead(200);
    res.end(JSON.stringify({ status: 'ok', engine: 'Container Preview (C# solution in backend/)' }));
    return;
  }

  res.writeHead(200);
  res.end(JSON.stringify(payload, null, 2));
});

server.listen(port, '0.0.0.0', () => {
  console.log(`Preview endpoint listening on http://0.0.0.0:${port}`);
});
