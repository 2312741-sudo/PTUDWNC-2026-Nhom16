"""Kiểm thử HTTP với PostgreSQL thật. Chỉ dọn các tài nguyên do lần chạy này tạo."""
import json
import os
import uuid
from urllib.request import Request, urlopen
from urllib.error import HTTPError
from urllib.parse import urlencode
from pathlib import Path
from datetime import datetime

BASE = os.environ.get('API_URL', 'http://localhost:5081').rstrip('/')
checks = []
categories, recipes = [], []

def check(condition, label):
    assert condition, label
    checks.append(label)
    print('PASS', label)

def req(method, path, body=None, status=200, raw=False):
    data = json.dumps(body).encode() if body is not None else None
    request = Request(BASE + path, data=data, method=method, headers={'Content-Type':'application/json'})
    try:
        response = urlopen(request, timeout=20)
    except HTTPError as e:
        response = e
    payload = response.read().decode()
    assert response.status == status, f'{method} {path}: expected {status}, got {response.status}: {payload}'
    if status >= 400:
        assert 'application/problem+json' in response.headers.get('Content-Type',''), payload
        assert json.loads(payload)['status'] == status
    return (payload if raw else json.loads(payload) if payload else None), response.headers

try:
    scalar,_ = req('GET','/scalar/v1',raw=True)
    check('scalar' in scalar.lower() and 'openapi' in scalar.lower(), 'Scalar trả HTML và tham chiếu OpenAPI')
    spec,_ = req('GET','/openapi/v1.json')
    operations = [v for p in spec['paths'].values() for k,v in p.items() if k in ['get','post','put','delete']]
    check(len(operations)==11, 'OpenAPI có đủ 11 thao tác')
    check(all(o.get('summary') and o.get('description') and o.get('responses') for o in operations), '11 thao tác có summary, description, response metadata')
    (Path(__file__).resolve().parents[1]/'docs'/'openapi.v1.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2))
    token = uuid.uuid4().hex[:10]
    cat,_ = req('POST','/api/v1/categories',{'name':'Món Tráng Miệng '+token,'description':'Kiểm thử'},201)
    categories.append(cat['id']); cid=cat['id']
    check(cat['slug']=='mon-trang-mieng-'+token, 'Category.Create tạo slug tiếng Việt đúng')
    cat2,_ = req('POST','/api/v1/categories',{'name':'Đồ Uống '+token,'description':None},201)
    categories.append(cat2['id'])
    req('POST','/api/v1/categories',{'name':'Món Tráng Miệng '+token},409)
    check(True,'Trùng slug trả 409')
    req('POST','/api/v1/categories',{'name':'   '},400)
    req('POST','/api/v1/categories',{},400)
    req('POST','/api/v1/categories',{'name':'!!!'},400)
    check(True,'Tên thiếu/rỗng/không tạo được slug trả 400')
    found,_=req('GET','/api/v1/categories?'+urlencode({'search':token,'pageSize':1,'sortBy':'name'}))
    check(found['totalCount']==2 and len(found['items'])==1 and found['totalPages']==2 and found['hasNextPage'], 'Category search và pagination metadata')
    updated,_=req('PUT',f'/api/v1/categories/{cid}',{'name':'Món Ngọt '+token,'description':'Đã cập nhật'})
    again,_=req('PUT',f'/api/v1/categories/{cid}',{'name':'Món Ngọt '+token,'description':'Đã cập nhật'})
    check(updated==again and updated['createdAt']==cat['createdAt'] and updated['updatedAt'], 'Category.Update giữ CreatedAt và PUT lặp không đổi trạng thái')
    req('PUT',f'/api/v1/categories/{cid}',{'name':cat2['name']},409)
    check(True,'Update trùng slug trả 409')
    detail,_=req('GET',f'/api/v1/categories/{cid}')
    check(detail['name']==updated['name'],'Update lỗi không lưu thay đổi')
    base={'title':'Cơm '+token,'description':'Mô tả','instructions':'Nấu và phục vụ','prepTimeMinutes':10,'cookTimeMinutes':15,'servings':2,'difficulty':'Easy','categoryId':cid,'authorId':str(uuid.uuid4())}
    for title,minutes,difficulty,category in [('A',15,'Easy',cid),('B',30,'Medium',cid),('C',45,'Hard',cat2['id']),('D',25,'Easy',cid)]:
        r,headers=req('POST','/api/v1/recipes',base|{'title':title+' '+token,'cookTimeMinutes':minutes,'difficulty':difficulty,'categoryId':category},201)
        recipes.append(r['id'])
        check(headers['Location']==f"/api/v1/recipes/{r['id']}" and r['category']['id']==category, f'POST Recipe {title}: 201, Location, Category DTO')
    rid=recipes[0]
    detail,_=req('GET',f'/api/v1/recipes/{rid}')
    check(detail['instructions']==base['instructions'] and detail['category']['id']==cid,'GetRecipeById có Instructions và Category')
    filtered,_=req('GET','/api/v1/recipes?'+urlencode({'search':token,'categoryId':cid,'difficulty':'Easy','minCookTime':15,'maxCookTime':25,'sortBy':'cooktimeminutes','descending':'true','pageSize':1}))
    check(filtered['totalCount']==2 and filtered['items'][0]['cookTimeMinutes']==25 and filtered['hasNextPage'],'Kết hợp category/difficulty/time/search/sort/pagination')
    check('instructions' not in filtered['items'][0] and 'category' not in filtered['items'][0], 'Danh sách dùng RecipeDto gọn')
    second,_=req('GET','/api/v1/recipes?'+urlencode({'search':token,'categoryId':cid,'difficulty':'Easy','minCookTime':15,'maxCookTime':25,'sortBy':'cooktimeminutes','descending':'true','pageSize':1,'page':2}))
    check(second['items'][0]['cookTimeMinutes']==15 and second['hasPreviousPage'] and not second['hasNextPage'],'Trang 2 và hai biên thời gian inclusive')
    nested,_=req('GET',f'/api/v1/categories/{cid}/recipes?categoryId={cat2["id"]}')
    check(nested['totalCount']==3 and all(r['categoryId']==cid for r in nested['items']),'Nested resource ưu tiên ID từ path')
    changed,_=req('PUT',f'/api/v1/recipes/{rid}',base|{'title':'Đã sửa '+token,'servings':4,'categoryId':cat2['id']})
    same,_=req('PUT',f'/api/v1/recipes/{rid}',base|{'title':'Đã sửa '+token,'servings':4,'categoryId':cat2['id']})
    check(changed==same and changed['category']['id']==cat2['id'] and changed['servings']==4 and changed['createdAt']==detail['createdAt'], 'Recipe.Update chuyển Category và PUT idempotent')
    for patch in [{'servings':0},{'cookTimeMinutes':-1},{'prepTimeMinutes':-1},{'title':''},{'instructions':''},{'authorId':str(uuid.UUID(int=0))},{'difficulty':'Unknown'},{'difficulty':99}]:
        req('POST','/api/v1/recipes',base|patch,400)
    check(True,'Validation Recipe: khẩu phần/thời gian/title/instructions/author/enum')
    req('POST','/api/v1/recipes',base|{'categoryId':str(uuid.uuid4())},404)
    check(True,'Recipe với Category không tồn tại trả 404')
    for route in ['/api/v1/categories?page=0','/api/v1/categories?pageSize=101','/api/v1/categories?sortBy=invalid','/api/v1/recipes?minCookTime=30&maxCookTime=10','/api/v1/recipes?difficulty=99','/api/v1/recipes?minCookTime=-1','/api/v1/recipes?pageSize=0','/api/v1/recipes?page=2147483647&pageSize=100','/api/v1/recipes?sortBy=invalid']:
        req('GET',route,status=400)
    check(True,'Query không hợp lệ trả ProblemDetails 400')
    for resource in ['categories','recipes']:
        missing=f'/api/v1/{resource}/{uuid.uuid4()}'
        req('GET',missing,status=404);req('DELETE',missing,status=404)
        req('PUT',missing,{'name':'abc'} if resource=='categories' else base,404)
    req('GET',f'/api/v1/categories/{uuid.uuid4()}/recipes',status=404)
    check(True,'GET/PUT/DELETE và nested ID không tồn tại trả 404')
    req('DELETE',f'/api/v1/categories/{cid}',status=409)
    check(True,'Không xóa Category còn Recipe')
    empty,_=req('GET','/api/v1/recipes?'+urlencode({'search':token,'page':100}))
    check(empty['items']==[] and empty['totalCount']==4,'Trang vượt giới hạn trả items rỗng, totalCount thật')
    for r in recipes[:]:
        req('DELETE',f'/api/v1/recipes/{r}',status=204);recipes.remove(r)
        req('GET',f'/api/v1/recipes/{r}',status=404)
    for c in categories[:]:
        req('DELETE',f'/api/v1/categories/{c}',status=204);categories.remove(c)
        req('DELETE',f'/api/v1/categories/{c}',status=404)
    check(True,'DELETE 204, kiểm tra đã xóa và DELETE lặp 404')
    report='# Kết quả kiểm thử thực tế\n\nThời điểm: '+datetime.now().astimezone().isoformat()+'\n\n'
    report+='API: '+BASE+'; database PostgreSQL thật `ptudwnc_chuong01`.\n\n'
    report+=f'**{len(checks)} nhóm kiểm tra đạt.**\n\n'+''.join('- PASS: '+x+'\n' for x in checks)
    report+='\nDữ liệu kiểm thử đã được dọn sau khi chạy. Scalar kiểm tra ở mức HTTP HTML và OpenAPI; chưa tự động thao tác giao diện trình duyệt.\n'
    (Path(__file__).resolve().parents[1]/'docs'/'Ket_qua_kiem_thu.md').write_text(report)
    print(f'ALL {len(checks)} CHECK GROUPS PASSED')
finally:
    for r in recipes:
        try: req('DELETE',f'/api/v1/recipes/{r}',status=204)
        except Exception as e: print('Cleanup recipe:',e)
    for c in categories:
        try: req('DELETE',f'/api/v1/categories/{c}',status=204)
        except Exception as e: print('Cleanup category:',e)
