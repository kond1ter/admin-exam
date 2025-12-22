pipeline {
    agent any

    environment {
        DOCKER_COMPOSE = 'docker-compose'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Docker Images') {
            steps {
                sh '''
                    docker-compose build
                '''
            }
        }

        stage('Stop Existing Containers') {
            steps {
                sh '''
                    docker-compose down || true
                '''
            }
        }

        stage('Start Services') {
            steps {
                sh '''
                    docker-compose up -d
                '''
            }
        }

        stage('Health Check') {
            steps {
                script {
                    sleep(time: 30, unit: 'SECONDS')
                    sh '''
                        echo "Checking services health..."
                        curl -f http://localhost:5001/swagger || exit 1
                        curl -f http://localhost:5002/swagger || exit 1
                        curl -f http://localhost:9090/-/healthy || exit 1
                        curl -f http://localhost:3000/api/health || exit 1
                        echo "All services are healthy!"
                    '''
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline completed'
        }
        success {
            echo 'Pipeline succeeded!'
        }
        failure {
            echo 'Pipeline failed!'
            sh 'docker-compose logs'
        }
    }
}

