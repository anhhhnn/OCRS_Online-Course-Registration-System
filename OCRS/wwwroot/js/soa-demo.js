(() => {
    const root = document.querySelector('#soa-demo');
    if (!root) return;
    const output = document.querySelector('#json-output');
    const status = document.querySelector('#http-status');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const pretty = value => JSON.stringify(value, null, 2);

    async function request(url, options = {}) {
        const headers = { Accept: 'application/json', ...(options.headers || {}) };
        if (token && options.method && options.method !== 'GET') headers.RequestVerificationToken = token;
        if (options.body) headers['Content-Type'] = 'application/json';
        const response = await fetch(url, { credentials: 'same-origin', ...options, headers });
        let body;
        try { body = await response.json(); } catch { body = { message: await response.text() || 'Không có nội dung.' }; }
        status.textContent = `${response.status} ${response.statusText}`;
        status.className = response.ok ? 'is-ok' : 'is-error';
        output.textContent = pretty(body);
        document.querySelector('#service-console').scrollIntoView({ behavior: 'smooth', block: 'center' });
        return { response, body };
    }

    async function loadCourses(show = true) {
        const keyword = document.querySelector('#course-keyword').value.trim();
        const url = keyword ? `/api/soa/courses/search?keyword=${encodeURIComponent(keyword)}` : '/api/soa/courses';
        const result = await request(url);
        if (result.response.ok) {
            document.querySelector('#course-results').innerHTML = result.body.map(x => `<button data-course="${x.courseId}"><strong>${x.courseCode}</strong><span>${x.courseName}</span><small>${x.learningMode} · ${Number(x.tuitionFee).toLocaleString('vi-VN')} đ</small></button>`).join('') || '<p>Không có kết quả.</p>';
            const select = document.querySelector('#course-select');
            select.innerHTML = '<option value="">Chọn khóa học</option>' + result.body.map(x => `<option value="${x.courseId}">${x.courseCode} · ${x.courseName}</option>`).join('');
        }
        if (!show) document.querySelector('#course-lab').scrollIntoView({ behavior: 'smooth' });
        return result.body;
    }

    async function loadClasses(courseId) {
        if (!courseId) return;
        const result = await request(`/api/soa/courses/${courseId}/classes`);
        if (!result.response.ok) return;
        document.querySelector('#class-results').innerHTML = result.body.map(x => `<button data-class="${x.classId}"><strong>${x.classCode}</strong><span>${x.className}</span><small>${x.remainingCapacity}/${x.maxCapacity} chỗ · ${x.registrationStatus}</small></button>`).join('') || '<p>Khóa học chưa có lớp.</p>';
        document.querySelector('#class-select').innerHTML = '<option value="">Chọn lớp học</option>' + result.body.filter(x => x.registrationStatus === 'OpenForRegistration').map(x => `<option value="${x.classId}">${x.classCode} · còn ${x.remainingCapacity} chỗ</option>`).join('');
    }

    document.addEventListener('click', async event => {
        const call = event.target.closest('[data-call]')?.dataset.call;
        if (call === 'courses') await loadCourses();
        if (call === 'classes') document.querySelector('#class-lab').scrollIntoView({ behavior: 'smooth' });
        if (call === 'registration') document.querySelector('#registration-lab').scrollIntoView({ behavior: 'smooth' });
        if (call === 'user') await request('/api/soa/users/me');
        if (call === 'notifications') await request('/api/soa/notifications/my');
        const classId = event.target.closest('[data-class]')?.dataset.class;
        if (classId) await request(`/api/soa/classes/${classId}/availability`);
    });
    document.querySelector('#load-courses').addEventListener('click', () => { document.querySelector('#course-keyword').value = ''; loadCourses(); });
    document.querySelector('#search-courses').addEventListener('click', () => loadCourses());
    document.querySelector('#load-classes').addEventListener('click', () => loadClasses(document.querySelector('#course-select').value));
    document.querySelector('#course-select').addEventListener('change', event => loadClasses(event.target.value));
    document.querySelector('#register-class').addEventListener('click', () => {
        const classId = Number(document.querySelector('#class-select').value);
        if (classId) request('/api/soa/registrations', { method: 'POST', body: JSON.stringify({ classId }) });
    });
    loadCourses();
})();
