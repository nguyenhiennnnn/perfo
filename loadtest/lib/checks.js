import { check } from 'k6';
import { Counter } from 'k6/metrics';

// Đếm riêng các phản hồi 409 Conflict (ví dụ tranh ghế khi giữ chỗ). 409 là kết quả nghiệp vụ hợp lệ
// khi nhiều người cùng giành một ghế, nên đếm tách khỏi lỗi thật.
export const conflict409 = new Counter('conflict_409');

// Đếm 429 (rate limit) để phân biệt "server từ chối vì giới hạn" với lỗi thật.
export const rateLimited429 = new Counter('rate_limited_429');

// Body JSON chuẩn của API: { success, data, message, errors, errorCode }. Trả null nếu không phải JSON.
export function parseBody(res) {
  try {
    return res.json();
  } catch (e) {
    return null;
  }
}

// Ghi nhận 409/429 vào counter, gắn tag name để tách theo endpoint. checkOk/checkOkOrConflict đã gọi
// hàm này: chỉ dùng MỘT trong số chúng cho mỗi response, nếu không counter bị đếm đôi.
export function trackStatus(res, name) {
  const tags = { name: name || 'unknown' };
  if (res.status === 409) conflict409.add(1, tags);
  if (res.status === 429) rateLimited429.add(1, tags);
}

// Check chuẩn: đúng status mong đợi (mặc định 200) và body có success === true.
// expectedStatus là một số hoặc mảng số (ví dụ [200, 201]). Trả true nếu mọi check đều qua.
export function checkOk(res, name, expectedStatus) {
  const expected = expectedStatus === undefined ? [200] : [].concat(expectedStatus);
  trackStatus(res, name);

  return check(res, {
    [`${name}: status ${expected.join('/')}`]: (r) => expected.indexOf(r.status) !== -1,
    [`${name}: body success=true`]: (r) => {
      const body = parseBody(r);
      return body !== null && body.success === true;
    },
  });
}

// Dùng khi 409 là kết quả hợp lệ (ví dụ giữ ghế đã có người giữ): thành công hoặc 409 đều qua,
// status khác là lỗi. 409 vẫn được đếm vào conflict_409.
export function checkOkOrConflict(res, name, successStatus) {
  const okStatus = successStatus === undefined ? [200] : [].concat(successStatus);
  trackStatus(res, name);

  return check(res, {
    [`${name}: status ${okStatus.join('/')} hoặc 409`]: (r) =>
      okStatus.indexOf(r.status) !== -1 || r.status === 409,
  });
}
