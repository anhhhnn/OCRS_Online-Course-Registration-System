(() => {
  'use strict';
  document.documentElement.classList.add('js-ready');

  const header = document.querySelector('[data-site-header]');
  const updateHeader = () => header?.classList.toggle('is-scrolled', window.scrollY > 12);
  updateHeader();
  window.addEventListener('scroll', updateHeader, { passive: true });

  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const revealItems = document.querySelectorAll('.reveal');
  if (reducedMotion || !('IntersectionObserver' in window)) {
    revealItems.forEach(item => item.classList.add('is-visible'));
  } else {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible');
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.1, rootMargin: '0px 0px -30px' });
    revealItems.forEach(item => observer.observe(item));
  }

  document.querySelectorAll('[data-password-toggle]').forEach(button => {
    button.addEventListener('click', () => {
      const field = button.closest('.password-field')?.querySelector('input');
      if (!field) return;
      const show = field.type === 'password';
      field.type = show ? 'text' : 'password';
      button.querySelector('span').textContent = show ? 'Ẩn' : 'Hiện';
      button.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    });
  });

  const password = document.querySelector('[data-password-strength]');
  const meter = document.querySelector('[data-password-meter]');
  password?.addEventListener('input', () => {
    const value = password.value;
    let level = 0;
    if (value.length >= 6) level++;
    if (/[A-Z]/.test(value) && /[a-z]/.test(value)) level++;
    if (/\d/.test(value)) level++;
    if (/[^A-Za-z0-9]/.test(value) && value.length >= 8) level++;
    meter?.setAttribute('data-level', level.toString());
  });

  document.querySelectorAll('[data-loading-form]').forEach(form => {
    form.addEventListener('submit', () => {
      if (typeof window.jQuery !== 'undefined' && !window.jQuery(form).valid()) return;
      const button = form.querySelector('button[type="submit"], button:not([type])');
      if (!button || button.disabled) return;
      button.classList.add('is-loading');
      button.setAttribute('aria-busy', 'true');
    });
  });

  document.querySelectorAll('form[data-confirm]').forEach(form => {
    form.addEventListener('submit', event => {
      if (!window.confirm(form.dataset.confirm)) event.preventDefault();
    });
  });
})();
